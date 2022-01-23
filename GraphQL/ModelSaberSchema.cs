using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;
using Microsoft.EntityFrameworkCore;
using ModelSaber.Database;
using ModelSaber.Models;

namespace ModelSaber.API.GraphQL
{
    public class ModelSaberSchema : Schema
    {
        public ModelSaberSchema(ModelSaberDbContext dbContext, IServiceProvider provider) : base(provider)
        {
            Query = new ModelSaberQuery(dbContext);
            Mutation = new ModelSaberMutation(dbContext);
        }
    }

    public class ModelSaberMutation : ObjectGraphType
    {
        public ModelSaberMutation(ModelSaberDbContext dbContext)
        {
            Field<IntGraphType>("vote", "Modifies votes for a model.", new QueryArguments(new QueryArgument<NonNullGraphType<VoteInputType>> { Name = "voteArgs" }), context =>
            {
                var args = context.GetArgument<VoteArgs>("voteArgs");
                if (args.IsDelete)
                {
                    var record = dbContext.Votes.FirstOrDefault(t => args.Platform == "web" ? t.UserId.ToString() == args.Id : t.GameId == args.Id && t.ModelId == args.ModelId);
                    if (record != null)
                        dbContext.Votes.Remove(record);
                }
                else
                {
                    var record = dbContext.Votes.FirstOrDefault(t => args.Platform == "web" ? t.UserId.ToString() == args.Id : t.GameId == args.Id);
                    if (record != null)
                        record.DownVote = args.IsDownVote;
                    else
                    {
                        dbContext.Votes.Add(new Vote
                        {
                            GameId = args.Platform != "web" ? args.Id : null,
                            UserId = args.Platform == "web" ? Convert.ToUInt32(args.Id) : null,
                            DownVote = args.IsDownVote,
                            ModelId = args.ModelId
                        });
                    }
                    dbContext.SaveChanges();
                }
                return 0;
            });
        }
    }

    public class ModelSaberQuery : ObjectGraphType
    {
        public ModelSaberQuery(ModelSaberDbContext dbContext)
        {
            Field<ListGraphType<UserType>>("users", "The entire user list", null,
                context => dbContext.Users.Include(t => t.Models)
                    .ThenInclude(t => t.Model)
                    .ThenInclude(t => t.Tags)
                    .ThenInclude(t => t.Tag)
                    .Include(t => t.UserTags));
            Field<ModelType>("model", "Single model", new QueryArguments(new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "id" }), context =>
              {
                  var id = context.GetArgument<Guid>("id");
                  return dbContext.Models.Where(t => t.Uuid == id).IncludeModelData().First();
              });

            Field<ListGraphType<StringGraphType>>("modelCursors", "Lists cursors based on pagination size", new QueryArguments(new QueryArgument<IntGraphType> { Name = "size", DefaultValue = 100 }), context =>
            {
                var modelCursors = dbContext.Models.ToList().Select(t => t.Uuid).Select(Cursor.ToCursor).ToList();
                var size = context.GetArgument<int>("size");
                return modelCursors.Chunk(size).Select(t => t.Last());
            });

            Connection<ModelType>()
                .Name("models")
                .Description("Model list")
                .Bidirectional()
                .Argument<TypeType>("modelType", "The model type you want to grab.")
                .Argument<StringGraphType>("nameFilter", "The name to search for in the models list.")
                .PageSize(100)
                .ResolveAsync(context => ResolveModelConnectionAsync(dbContext,
                    d => d.Models,
                    (set, i, a, c) => set.GetModelAsync(i, a, context.GetArgument<string>("nameFilter"), (TypeEnum?)context.GetArgument(typeof(object), "modelType"), c),
                    (set, i, a, c) => set.GetModelReverseAsync(i, a, context.GetArgument<string>("nameFilter"), (TypeEnum?)context.GetArgument(typeof(object), "modelType"), c),
                    model => model.Uuid,
                    (set, c, id) => set.GetModelNextPageAsync(c, id),
                    (set, c, id) => set.GetModelPreviousPageAsync(c, id),
                    context));

            Connection<TagType>()
                .Name("tags")
                .Description("You wanted yer tags?")
                .Bidirectional()
                .Argument<StringGraphType>("nameFilter", "The name to search for in the tag list.")
                .PageSize(100)
                .ResolveAsync(context => ResolveModelConnectionAsync(dbContext,
                    d => d.Tags,
                    (set, i, a, c) => set.GetTagAsync(i, a, context.GetArgument<string>("nameFilter"), c),
                    (set, i, a, c) => set.GetTagReverseAsync(i, a, context.GetArgument<string>("nameFilter"), c),
                    model => model.CursorId,
                    (set, c, id) => set.GetTagNextPageAsync(c, id),
                    (set, c, id) => set.GetTagPreviousPageAsync(c, id),
                    context));
        }

        private async Task<object?> ResolveModelConnectionAsync<TR, TU>(ModelSaberDbContext dbContext,
            Func<ModelSaberDbContext, DbSet<TR>> func,
            Func<DbSet<TR>, int?, TU?, CancellationToken, Task<List<TR>>> beforeFunc,
            Func<DbSet<TR>, int?, TU?, CancellationToken, Task<List<TR>>> afterFunc,
            Func<TR, TU> cursorFunc,
            Func<DbSet<TR>, CancellationToken, uint?, Task<bool>> afterCheckFunc,
            Func<DbSet<TR>, CancellationToken, uint?, Task<bool>> beforeCheckFunc,
            IResolveConnectionContext<object?> context) where TR : BaseId where TU : struct
        {
            var first = context.First;
            var afterCursor = Cursor.FromCursor<TU>(context.After);
            var last = context.Last;
            var beforeCursor = Cursor.FromCursor<TU>(context.Before);
            var cancellationToken = context.CancellationToken;

            var list = await GetListAsync(dbContext, first, afterCursor, last, beforeCursor, cancellationToken, func, beforeFunc, afterFunc).ConfigureAwait(false);
            var getNextPageTask = GetNextPageAsync(dbContext, cancellationToken, func, list, afterCheckFunc);
            var getPreviousPageTask = GetPreviousPageAsync(dbContext, cancellationToken, func, list, beforeCheckFunc);
            var totalCountTask = Task.FromResult(func(dbContext).Count());

            var (nextPage, previousPage, totalCount) = await WaitAll(getNextPageTask, getPreviousPageTask, totalCountTask).ConfigureAwait(false);
            var (firstCursor, lastCursor) = Cursor.GetFirstAndLastCursor(list, cursorFunc);

            return new Connection<TR>
            {
                Edges = list.Select(x => new Edge<TR>
                {
                    Cursor = Cursor.ToCursor(cursorFunc(x)),
                    Node = x
                }).ToList(),
                PageInfo = new PageInfo
                {
                    HasNextPage = nextPage,
                    HasPreviousPage = previousPage,
                    StartCursor = firstCursor,
                    EndCursor = lastCursor
                },
                TotalCount = totalCount
            };
        }

        private Task<bool> GetPreviousPageAsync<TR>(ModelSaberDbContext dbContext,
            CancellationToken cancellationToken,
            Func<ModelSaberDbContext, DbSet<TR>> modelFunc,
            List<TR> list,
            Func<DbSet<TR>, CancellationToken, uint?, Task<bool>> func) where TR : BaseId
            => func(modelFunc(dbContext), cancellationToken, list.FirstOrDefault()?.Id);

        private Task<bool> GetNextPageAsync<TR>(ModelSaberDbContext dbContext,
            CancellationToken cancellationToken,
            Func<ModelSaberDbContext, DbSet<TR>> modelFunc,
            List<TR> list,
            Func<DbSet<TR>, CancellationToken, uint?, Task<bool>> func) where TR : BaseId
            => func(modelFunc(dbContext), cancellationToken, list.LastOrDefault()?.Id);

        private Task<List<TR>> GetListAsync<TR, TU>(ModelSaberDbContext dbContext,
            int? first,
            TU? afterCursor,
            int? last,
            TU? beforeCursor,
            CancellationToken cancellationToken,
            Func<ModelSaberDbContext, DbSet<TR>> dbFunc,
            Func<DbSet<TR>, int?, TU?, CancellationToken, Task<List<TR>>> beforeFunc,
            Func<DbSet<TR>, int?, TU?, CancellationToken, Task<List<TR>>> afterFunc) where TR : class
            => first.HasValue ?
                beforeFunc(dbFunc(dbContext), first, afterCursor, cancellationToken) :
                afterFunc(dbFunc(dbContext), last, beforeCursor, cancellationToken);

        private async Task<(T1, T2, T3)> WaitAll<T1, T2, T3>(Task<T1> task1, Task<T2> task2, Task<T3> task3)
        {
            await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);
            return (await task1.ConfigureAwait(false), await task2.ConfigureAwait(false), await task3.ConfigureAwait(false));
        }
    }
}