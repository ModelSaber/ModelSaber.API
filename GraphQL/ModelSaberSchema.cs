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
using ModelSaber.Database.Models;

namespace ModelSaber.API.GraphQL
{
    public class ModelSaberSchema : Schema
    {
        public ModelSaberSchema(ModelSaberDbContext dbContext, IServiceProvider provider) : base(provider)
        {
            Query = new ModelSaberQuery(dbContext);
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
            Field<ModelType>("model", "Single model", new QueryArguments(new QueryArgument<NonNullGraphType<IdGraphType>> {Name = "id"}), context =>
            {
                var id = context.GetArgument<Guid>("id");
                return dbContext.Models.Where(t => t.Uuid == id).IncludeModelData().First();
            });

            Connection<ModelType>()
                .Name("models")
                .Description("Model list")
                .Bidirectional()
                .Argument<TypeType>("modelType", "The model type you want to grab.")
                .PageSize(100)
                .ResolveAsync(context => ResolveModelConnectionAsync(dbContext,
                    d => d.Models,
                    (set, i, a, c) => set.GetModelAsync(i, a, (TypeEnum?)context.GetArgument(typeof(object), "modelType"), c),
                    (set, i, a, c) => set.GetModelReverseAsync(i, a, (TypeEnum?)context.GetArgument(typeof(object), "modelType"), c),
                    model => model.Uuid,
                    (set, i, a, c) => set.GetModelNextPageAsync(i,a,c),
                    (set, i, a, c) => set.GetModelPreviousPageAsync(i,a,c),
                    context));

            Connection<TagType>()
                .Name("tags")
                .Description("You wanted yer tags?")
                .Bidirectional()
                .PageSize(100)
                .ResolveAsync(context => ResolveModelConnectionAsync(dbContext,
                    d => d.Tags,
                    (set, i, a, c) => set.GetTagAsync(i,a,c),
                    (set, i, a, c) => set.GetTagReverseAsync(i, a, c),
                    model => model.CursorId,
                    (set, i, a, c) => set.GetTagNextPageAsync(i,a,c),
                    (set, i, a, c) => set.GetTagPreviousPageAsync(i,a,c),
                    context));
        }

        private async Task<object?> ResolveModelConnectionAsync<TR, TU>(ModelSaberDbContext dbContext, 
            Func<ModelSaberDbContext, DbSet<TR>> func, 
            Func<DbSet<TR>, int?, TU?, CancellationToken, Task<List<TR>>> beforeFunc, 
            Func<DbSet<TR>, int?, TU?, CancellationToken, Task<List<TR>>> afterFunc, 
            Func<TR, TU> cursorFunc, 
            Func<DbSet<TR>, int?, TU?, CancellationToken, Task<bool>> afterCheckFunc,
            Func<DbSet<TR>, int?, TU?, CancellationToken, Task<bool>> beforeCheckFunc,
            IResolveConnectionContext<object?> context) where TR : class where TU : struct
        {
            var first = context.First;
            var afterCursor = Cursor.FromCursor<TU>(context.After);
            var last = context.Last;
            var beforeCursor = Cursor.FromCursor<TU>(context.Before);
            var cancellationToken = context.CancellationToken;

            var getModelsTask = GetListAsync(dbContext, first, afterCursor, last, beforeCursor, cancellationToken, func, beforeFunc, afterFunc);
            var getNextPageTask = GetNextPageAsync(dbContext, first, afterCursor, cancellationToken, func, afterCheckFunc);
            var getPreviousPageTask = GetPreviousPageAsync(dbContext, last, beforeCursor, cancellationToken, func, beforeCheckFunc);
            var totalCountTask = Task.FromResult(func(dbContext).Count());

            var (models, nextPage, previousPage, totalCount) = await WaitAll(getModelsTask, getNextPageTask, getPreviousPageTask, totalCountTask).ConfigureAwait(false);
            var (firstCursor, lastCursor) = Cursor.GetFirstAndLastCursor(models, cursorFunc);

            return new Connection<TR>
            {
                Edges = models.Select(x => new Edge<TR>
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

        private Task<bool> GetPreviousPageAsync<TR, TU>(ModelSaberDbContext dbContext, 
            int? last, 
            TU? beforeCursor, 
            CancellationToken cancellationToken, 
            Func<ModelSaberDbContext, DbSet<TR>> modelFunc,
            Func<DbSet<TR>, int?, TU?, CancellationToken, Task<bool>> func) where TR : class
            => func(modelFunc(dbContext), last, beforeCursor, cancellationToken);

        private Task<bool> GetNextPageAsync<TR, TU>(ModelSaberDbContext dbContext, 
            int? first, 
            TU? afterCursor, 
            CancellationToken cancellationToken,
            Func<ModelSaberDbContext, DbSet<TR>> modelFunc,
            Func<DbSet<TR>, int?, TU?, CancellationToken, Task<bool>> func) where TR : class
            => func(modelFunc(dbContext), first, afterCursor, cancellationToken);

        private Task<List<TR>> GetListAsync<TR, TU>(ModelSaberDbContext dbContext, int? first, TU? afterCursor, int? last, TU? beforeCursor, CancellationToken cancellationToken, Func<ModelSaberDbContext, DbSet<TR>> dbFunc, Func<DbSet<TR>, int?, TU?, CancellationToken, Task<List<TR>>> beforeFunc, Func<DbSet<TR>, int?, TU?, CancellationToken, Task<List<TR>>> afterFunc) where TR : class
            => first.HasValue ?
                beforeFunc(dbFunc(dbContext), first, afterCursor, cancellationToken) :
                afterFunc(dbFunc(dbContext), last, beforeCursor, cancellationToken);

        private async Task<(T1, T2, T3, T4)> WaitAll<T1, T2, T3, T4>(Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4)
        {
            await Task.WhenAll(task1, task2, task3, task4).ConfigureAwait(false);
            return (await task1.ConfigureAwait(false), await task2.ConfigureAwait(false), await task3.ConfigureAwait(false), await task4.ConfigureAwait(false));
        }
    }
}