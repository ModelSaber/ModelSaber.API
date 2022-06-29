using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelSaber.Database;
using ModelSaber.Models;

namespace ModelSaber.API.GraphQL
{
    public class ModelSaberSchema : Schema
    {
        public ModelSaberSchema(ModelSaberDbContextLeaser dbContextLeaser, IServiceProvider provider) : base(provider)
        {
            Query = new ModelSaberQuery(dbContextLeaser);
            Mutation = new ModelSaberMutation(dbContextLeaser);
        }
    }

    public class ModelSaberMutation : ObjectGraphType
    {
        public ModelSaberMutation(ModelSaberDbContextLeaser dbContextLeaser)
        {
            Field<VoteType>("vote", "Modifies votes for a model.", new QueryArguments(new QueryArgument<NonNullGraphType<VoteInputType>> { Name = "voteArgs" }), context =>
            {
                var args = context.GetArgument<VoteArgs>("voteArgs");
                //this is just a jank workaround for some reason it does not like dependency injection right here
                using var dbContext = dbContextLeaser.GetContext();
                var id = args.Platform == "web" ? dbContext.Users.First(t => t.DiscordId == Convert.ToUInt64(args.Id)).Id.ToString() : args.Id;
                var record = dbContext.Votes.ToList().FirstOrDefault(t => args.Platform == "web" ? t.UserId.ToString() == id : t.GameId == id && t.ModelId == args.ModelId);
                Vote? ret = null;
                if (args.IsDelete)
                {
                    if (record != null)
                        dbContext.Votes.Remove(record);
                }
                else
                {
                    ret = record;
                    if (record != null)
                        record.DownVote = args.IsDownVote;
                    else
                    {
                        ret = new Vote
                        {
                            GameId = args.Platform != "web" ? id : null,
                            UserId = args.Platform == "web" ? Convert.ToUInt32(id) : null,
                            DownVote = args.IsDownVote,
                            ModelId = args.ModelId
                        };
                        dbContext.Votes.Add(ret);
                    }
                }
                dbContext.SaveChanges();

                return ret;
            });
        }
    }

    public class ModelSaberQuery : ObjectGraphType
    {
        public ModelSaberQuery(ModelSaberDbContextLeaser dbContextLeaser)
        {
            Field<StringGraphType>("version", "Returns the current version of the API.", resolve: _ => Program.Version.ToString(3));
            Field<DateTimeGraphType>("buildTime", "Returns the time the API was built.", resolve: _ => Program.CompiledTime);
            Field<VoteType>("modelVote", "Gets the current users vote.", new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }), context =>
            {
                var id = context.GetArgument<string>("id");
                var guid = Cursor.FromCursor<Guid>(id);
                var auth = context.UserContext["auth"];
                Console.WriteLine(auth);
                var userId = auth switch
                {
                    UserLogons user => user.UserId,
                    OAuthToken { UserId: { } } token => token.UserId.Value,
                    _ => throw new NullReferenceException("Tried to get an uint from null")
                };
                return dbContextLeaser.GetContext().Votes.Include(t => t.Model).FirstOrDefault(t => t.UserId == userId && t.Model.Uuid == guid);
            }).RequiresPermission();

            Field<ListGraphType<UserType>>("users", "The entire user list", null,
                context => dbContextLeaser.GetContext().Users.Include(t => t.Models)
                    .ThenInclude(t => t.Model)
                    .ThenInclude(t => t.Tags)
                    .ThenInclude(t => t.Tag)
                    .Include(t => t.UserTags));

            Field<ListGraphType<VoteCompoundType>>("modelVotes", "Gets the vote stats for the model.", new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }), context =>
            {
                var id = context.GetArgument<string>("id");
                var guid = Cursor.FromCursor<Guid>(id);
                var votes = dbContextLeaser.GetContext().Models.Include(t => t.Votes).Where(t => t.Uuid == guid).SelectMany(t => t.Votes).ToList();
                return votes.GroupBy(t => t.DownVote).Select(t => new ModelVoteCondensed { Down = t.Key, Count = t.Count() });
            });

            Field<ModelType>("model", "Single model", new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }), context =>
            {
                var id = context.GetArgument<string>("id");
                var guid = Cursor.FromCursor<Guid>(id);
                return dbContextLeaser.GetContext().Models.Where(t => t.Uuid == guid).IncludeModelData().First();
            });

            Field<ListGraphType<StringGraphType>>("modelCursors", "Lists cursors based on pagination size", new QueryArguments(
                new QueryArgument<IntGraphType> { Name = "size", DefaultValue = 100 },
                new QueryArgument<StringGraphType> { Name = "order", DefaultValue = "asc", Description = "Sort order for models either 'asc' or 'desc'" },
                new QueryArgument<ListGraphType<StatusType>> { Name = "status", DefaultValue = new List<Status> { Status.Approved, Status.Published }, Description = "The status of the model you want to grab. Defaults to Approved and Published."},
                new QueryArgument<BooleanGraphType>{Name = "nsfw", DefaultValue = false, Description = "Whether or not to include nsfw models in the list. Defaults to false"},
                new QueryArgument<PlatformType>{Name = "platform", DefaultValue = Platform.Pc, Description = "The platform you want to grab. Defaults to PC."},
                new QueryArgument<TypeType>{Name = "type", Description = "The model type you want to grab. Defaults to all."}), context =>
            {
                using var dbContext = dbContextLeaser.GetContext();
                var status = context.GetArgument<List<Status>>("status").GetFlagFromList();
                var nsfw = context.GetArgument<bool>("nsfw");
                var platform = context.GetArgument<Platform>("platform");
                var type = context.GetArgument<TypeEnum?>("type");
                var models = (context.GetArgument<string>("order") == "asc" ? dbContext.Models.OrderBy(t => t.Id) : dbContext.Models.OrderByDescending(t => t.Id)).Where(t => (t.Status & status) == status);
                var modelsFilter = new List<Model>();
                modelsFilter.AddRange(models.Where(t => t.Nsfw == false && t.Platform == platform));
                if(nsfw) modelsFilter.AddRange(models.Where(t => t.Nsfw == true && t.Platform == platform));
                var modelCursors = modelsFilter.ToList().If(type.HasValue, x => x.Where(t => t.Type == type!.Value)).Select(t => t.Uuid).Select(Cursor.ToCursor).ToList();
                var size = context.GetArgument<int>("size");
                return modelCursors.Chunk(size).Select(t => t.Last()).SkipLast(1);
            });

            Connection<ModelType>()
                .Name("models")
                .Description("Model list")
                .Bidirectional()
                .Argument<TypeType>("modelType", "The model type you want to grab. Defaults to all.")
                .Argument<StringGraphType>("nameFilter", "The name to search for in the models list. (can be empty string)", argument => argument.DefaultValue = "")
                .Argument<BooleanGraphType, bool>("nsfw", "Whether or not to include nsfw models in the list. Defaults to false.")
                .Argument<ListGraphType<StatusType>>("status", "The status of the model you want to grab. Defaults to Approved and Published.", argument => argument.DefaultValue = new List<Status> { Status.Approved, Status.Published })
                .Argument<PlatformType>("platform", "The platform you want to grab. Defaults to PC.", argument => argument.DefaultValue = Platform.Pc)
                .PageSize(100)
                .ResolveAsync(context => ResolveModelConnectionAsync(dbContextLeaser.GetContext(),
                    d => d.Models,
                    (dbSet, first, after, cancellationToken) => dbSet.GetModelAsync(first, 
                        after, 
                        context.GetArgument<string>("nameFilter"), 
                        context.GetArgument<TypeEnum?>("modelType"), 
                        context.GetArgument<bool>("nsfw"), 
                        context.GetArgument<List<Status>>("status").GetFlagFromList(), 
                        context.GetArgument<Platform>("platform"), 
                        cancellationToken),
                    (dbSet, last, before, cancellationToken) => dbSet.GetModelReverseAsync(last, 
                        before, 
                        context.GetArgument<string>("nameFilter"), 
                        context.GetArgument<TypeEnum?>("modelType"), 
                        context.GetArgument<bool>("nsfw"), 
                        context.GetArgument<List<Status>>("status").GetFlagFromList(), 
                        context.GetArgument<Platform>("platform"), 
                        cancellationToken),
                    model => model.Uuid,
                    (dbSet, cancellationToken, id) => dbSet.GetModelNextPageAsync(cancellationToken, id),
                    (dbSet, cancellationToken, id) => dbSet.GetModelPreviousPageAsync(cancellationToken, id),
                    context));

            Connection<TagType>()
                .Name("tags")
                .Description("You wanted yer tags?")
                .Bidirectional()
                .Argument<StringGraphType>("nameFilter", "The name to search for in the tag list.")
                .PageSize(100)
                .ResolveAsync(context => ResolveModelConnectionAsync(dbContextLeaser.GetContext(),
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