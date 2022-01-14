using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Builders;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;
using ModelSaber.Database.Models;

namespace ModelSaber.API.GraphQL
{
    public sealed class ModelType : ObjectGraphType<Model>
    {
        public ModelType()
        {
            Field(o => o.Date, type: typeof(DateTimeGraphType));
            Field(o => o.Hash, type: typeof(StringGraphType));
            Field(o => o.Id);
            Field(o => o.Name);
            Field(o => o.Description, true);
            Field(o => o.Platform, type: typeof(PlatformType));
            Field(o => o.Status, type: typeof(StatusType));
            Field(o => o.Type, type: typeof(TypeType));
            Field(o => o.Thumbnail);
            Field(o => o.Uuid);
            Field(o => o.DownloadPath);
            Field(o => o.UserId, type: typeof(ULongGraphType));
            Field<ListGraphType<TagType>>("tags", resolve: context => context.Source?.Tags.Select(t => t.Tag));
            Field<ListGraphType<UserType>>("users", resolve: context => context.Source?.Users.Select(t => t.User));
            Field<UserType>("mainUser", resolve: context => context.Source?.User);
        }
    }

    public sealed class TagType : ObjectGraphType<Tag>
    {
        public TagType()
        {
            Field(o => o.Id);
            Field(o => o.Name);
            Connection<ModelType>()
                .Name("models")
                .Description("Model list")
                .Bidirectional()
                .PageSize(100)
                .ResolveAsync(context => context.ResolveConnectionAsync(connectionContext => connectionContext.Source?.ModelTags.Select(t => t.Model)!, 
                    (set, i, u, c) => Task.FromResult(set.If(u.HasValue, x => x.Where(y => y.Date > u!.Value)).TakeLast(i!.Value).ToList()),
                    (set, i, u, c) => Task.FromResult(set.If(u.HasValue, x => x.Where(y => y.Date > u!.Value)).Take(i!.Value).ToList()),
                    set => set?.Date,
                    (set, i, u, c) => Task.FromResult(set.If(u.HasValue, x => x.Where(y => y.Date > u!.Value)).If(i.HasValue, x => x.Take(i!.Value)).Any()),
                    (set, i, u, c) => Task.FromResult(set.If(u.HasValue, x => x.Where(y => y.Date < u!.Value)).If(i.HasValue, x => x.TakeLast(i!.Value)).Any())));
        }
    }

    public class PlatformType : EnumerationGraphType<Platform>
    {
    }

    public class StatusType : EnumerationGraphType<Status>
    {
    }

    public class TypeType : EnumerationGraphType<TypeEnum>
    {
    }

    public class UserLevelType : EnumerationGraphType<UserLevel>
    {
    }

    public sealed class UserType : ObjectGraphType<User>
    {
        public UserType()
        {
            Field(o => o.BSaber, true, typeof(StringGraphType));
            Field(o => o.DiscordId, type: typeof(ULongGraphType));
            Field(o => o.Level, type: typeof(UserLevelType));
            Field(o => o.Name, type: typeof(StringGraphType));
            Field(o => o.Avatar, true, typeof(StringGraphType));
            Field<ListGraphType<UserTagType>>("userTags", resolve: context => context.Source?.UserTags.ToList());
            Connection<ModelType>()
                .Name("models")
                .Description("Model list")
                .Bidirectional()
                .PageSize(100)
                .ResolveAsync(context => context.ResolveConnectionAsync(connectionContext => connectionContext.Source?.Models.Select(t => t.Model)!, 
                    (set, i, u, c) => Task.FromResult(set.If(u.HasValue, x => x.Where(y => y.Date < u!.Value)).TakeLast(i!.Value).ToList()),
                    (set, i, u, c) => Task.FromResult(set.If(u.HasValue, x => x.Where(y => y.Date > u!.Value)).Take(i!.Value).ToList()),
                    set => set?.Date,
                    (set, i, u, c) => Task.FromResult(set.If(u.HasValue, x => x.Where(y => y.Date > u!.Value)).If(i.HasValue, x => x.Take(i!.Value)).Any()),
                    (set, i, u, c) => Task.FromResult(set.If(u.HasValue, x => x.Where(y => y.Date < u!.Value)).If(i.HasValue, x => x.TakeLast(i!.Value)).Any())));
        }
    }

    public sealed class UserTagType : ObjectGraphType<UserTags>
    {
        public UserTagType()
        {
            Field(o => o.Name, type: typeof(StringGraphType));
        }
    }

    public static class GQLPaginationExtension
    {
        public static async Task<object?> ResolveConnectionAsync<TR, TU, TC>(this IResolveConnectionContext<TC> context, 
            Func<IResolveConnectionContext<TC>, IEnumerable<TR>> func, 
            Func<IEnumerable<TR>, int?, TU?, CancellationToken, Task<List<TR>>> beforeFunc, 
            Func<IEnumerable<TR>, int?, TU?, CancellationToken, Task<List<TR>>> afterFunc, 
            Func<TR, TU?> cursorFunc, 
            Func<IEnumerable<TR>, int?, TU?, CancellationToken, Task<bool>> afterCheckFunc,
            Func<IEnumerable<TR>, int?, TU?, CancellationToken, Task<bool>> beforeCheckFunc) where TC : class where TU : struct
        {
            var first = context.First;
            var afterCursor = Cursor.FromCursor<TU>(context.After);
            var last = context.Last;
            var beforeCursor = Cursor.FromCursor<TU>(context.Before);
            var cancellationToken = context.CancellationToken;

            var getModelsTask = GetListAsync(context, first, afterCursor, last, beforeCursor, cancellationToken, func, beforeFunc, afterFunc);
            var getNextPageTask = GetNextPageAsync(context, first, afterCursor, cancellationToken, func, afterCheckFunc);
            var getPreviousPageTask = GetPreviousPageAsync(context, last, beforeCursor, cancellationToken, func, beforeCheckFunc);
            var totalCountTask = Task.FromResult(func(context).Count());

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
        private static Task<bool> GetPreviousPageAsync<TR, TU, TC>(IResolveConnectionContext<TC> dbContext, 
            int? last, 
            TU? beforeCursor, 
            CancellationToken cancellationToken, 
            Func<IResolveConnectionContext<TC>, IEnumerable<TR>> modelFunc,
            Func<IEnumerable<TR>, int?, TU?, CancellationToken, Task<bool>> func) where TC : class
            => func(modelFunc(dbContext), last, beforeCursor, cancellationToken);

        private static Task<bool> GetNextPageAsync<TR, TU, TC>(IResolveConnectionContext<TC> dbContext, 
            int? first, 
            TU? afterCursor, 
            CancellationToken cancellationToken,
            Func<IResolveConnectionContext<TC>, IEnumerable<TR>> modelFunc,
            Func<IEnumerable<TR>, int?, TU?, CancellationToken, Task<bool>> func) where TC : class
            => func(modelFunc(dbContext), first, afterCursor, cancellationToken);

        private static Task<List<TR>> GetListAsync<TR, TU, TC>(IResolveConnectionContext<TC> context, 
            int? first, 
            TU? afterCursor, 
            int? last, 
            TU? beforeCursor, 
            CancellationToken cancellationToken, 
            Func<IResolveConnectionContext<TC>, IEnumerable<TR>> dbFunc, 
            Func<IEnumerable<TR>, int?, TU?, CancellationToken, Task<List<TR>>> beforeFunc, 
            Func<IEnumerable<TR>, int?, TU?, CancellationToken, Task<List<TR>>> afterFunc) where TC : class
            => first.HasValue ?
                beforeFunc(dbFunc(context), first, afterCursor, cancellationToken) :
                afterFunc(dbFunc(context), last, beforeCursor, cancellationToken);

        private static async Task<(T1, T2, T3, T4)> WaitAll<T1, T2, T3, T4>(Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4)
        {
            await Task.WhenAll(task1, task2, task3, task4).ConfigureAwait(false);
            return (await task1.ConfigureAwait(false), await task2.ConfigureAwait(false), await task3.ConfigureAwait(false), await task4.ConfigureAwait(false));
        }
    }
}