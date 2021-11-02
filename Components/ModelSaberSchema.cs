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

namespace ModelSaber.API.Components
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
            //Field<ListGraphType<TagType>>("tags", "The entire tag list endpoint", null, context => dbContext.Tags.Include(t => t.ModelTags).ThenInclude(t => t.Model).ThenInclude(t => t.Users).ThenInclude(t => t.User));
            Field<ListGraphType<UserType>>("users", "The entire user list", null, context => dbContext.Users.Include(t => t.Models).ThenInclude(t => t.Model).ThenInclude(t => t.Tags).ThenInclude(t => t.Tag));
            Field<ModelType>("model", "Single model", new QueryArguments(new QueryArgument<NonNullGraphType<IdGraphType>> {Name = "id"}), context =>
            {
                var id = context.GetArgument<Guid>("id");
                return dbContext.Models.Where(t => t.Uuid == id).IncludeModelData().First();
            });
            Connection<ModelType>()
                .Name("models")
                .Description("Model list")
                .Bidirectional()
                .PageSize(100)
                .ResolveAsync(context => ResolveModelConnectionAsync(dbContext, 
                    d => d.Models, 
                    (set, i, a, c) => set.GetModelAsync(i, a, c), 
                    (set, i, a, c) => set.GetModelReverseAsync(i, a, c),
                    model => model.Date,
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

        private async Task<object> ResolveModelConnectionAsync<T, U>(ModelSaberDbContext dbContext, 
            Func<ModelSaberDbContext, DbSet<T>> func, 
            Func<DbSet<T>, int?, U?, CancellationToken, Task<List<T>>> beforeFunc, 
            Func<DbSet<T>, int?, U?, CancellationToken, Task<List<T>>> afterFunc, 
            Func<T, U> cursorFunc, 
            Func<DbSet<T>, int?, U?, CancellationToken, Task<bool>> afterCheckFunc,
            Func<DbSet<T>, int?, U?, CancellationToken, Task<bool>> beforeCheckFunc,
            IResolveConnectionContext<object> context) where T : class
        {
            var first = context.First;
            var afterCursor = Cursor.FromCursor<U>(context.After);
            var last = context.Last;
            var beforeCursor = Cursor.FromCursor<U>(context.Before);
            var cancellationToken = context.CancellationToken;

            var getModelsTask = GetListAsync(dbContext, first, afterCursor, last, beforeCursor, cancellationToken, func, beforeFunc, afterFunc);
            var getNextPageTask = GetNextPageAsync(dbContext, first, afterCursor, cancellationToken, func, afterCheckFunc);
            var getPreviousPageTask = GetPreviousPageAsync(dbContext, last, beforeCursor, cancellationToken, func, beforeCheckFunc);
            var totalCountTask = Task.FromResult(func(dbContext).Count());

            var (models, nextPage, previousPage, totalCount) = await WaitAll(getModelsTask, getNextPageTask, getPreviousPageTask, totalCountTask).ConfigureAwait(false);
            var (firstCursor, lastCursor) = Cursor.GetFirstAndLastCursor(models, cursorFunc);

            return new Connection<T>
            {
                Edges = models.Select(x => new Edge<T>
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

        private Task<bool> GetPreviousPageAsync<T, U>(ModelSaberDbContext dbContext, 
            int? last, 
            U? beforeCursor, 
            CancellationToken cancellationToken, 
            Func<ModelSaberDbContext, DbSet<T>> modelFunc,
            Func<DbSet<T>, int?, U?, CancellationToken, Task<bool>> func) where T : class
            => func(modelFunc(dbContext), last, beforeCursor, cancellationToken);

        private Task<bool> GetNextPageAsync<T, U>(ModelSaberDbContext dbContext, 
            int? first, 
            U? afterCursor, 
            CancellationToken cancellationToken,
            Func<ModelSaberDbContext, DbSet<T>> modelFunc,
            Func<DbSet<T>, int?, U?, CancellationToken, Task<bool>> func) where T : class
            => func(modelFunc(dbContext), first, afterCursor, cancellationToken);

        private Task<List<T>> GetListAsync<T, U>(ModelSaberDbContext dbContext, int? first, U? afterCursor, int? last, U? beforeCursor, CancellationToken cancellationToken, Func<ModelSaberDbContext, DbSet<T>> dbFunc, Func<DbSet<T>, int?, U?, CancellationToken, Task<List<T>>> beforeFunc, Func<DbSet<T>, int?, U?, CancellationToken, Task<List<T>>> afterFunc) where T : class
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