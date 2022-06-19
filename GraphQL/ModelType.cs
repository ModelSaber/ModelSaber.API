using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;
using Microsoft.AspNetCore.WebUtilities;
using ModelSaber.Models;

namespace ModelSaber.API.GraphQL
{
    public sealed class ModelType : ObjectGraphType<Model>
    {
        public ModelType()
        {
            Field(o => o.Date, type: typeof(DateTimeGraphType));
            Field(o => o.Hash, type: typeof(StringGraphType));
            Field(o => o.Name);
            Field(o => o.Description, true);
            Field<PlatformType>("platform", "The platform the model is for", resolve: context => context.Source.Platform);
            Field<StatusType>("status", "The status of the model", resolve: context => context.Source.Status);
            Field<TypeType>("type", "What model type it is", resolve: context => context.Source.Type);
            Field(o => o.Thumbnail);
            Field<GuidGraphType>("id", "The id of the model", resolve: context => context.Source.Uuid);
            Field("cursor", o => WebEncoders.Base64UrlEncode(o.Uuid.ToByteArray()));
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
                .Argument<BooleanGraphType, bool>("nsfw", "Whether or not to include nsfw models in the list. Defaults to false.")
                .PageSize(100)
                .ResolveAsync(context => context.ResolveConnectionAsync(connectionContext => connectionContext.Source?.ModelTags.Select(t => t.Model).Where(t => t.Nsfw == context.GetArgument<bool>("nsfw"))!, 
                    (set, i, u, c) => Task.FromResult(set.OrderByDescending(t => t.Id).If(u.HasValue, x => x.SkipWhile(y => y.Uuid != u!.Value)).TakeLast(i!.Value).ToList()),
                    (set, i, u, c) => Task.FromResult(set.OrderBy(t => t.Id).If(u.HasValue, x => x.SkipWhile(y => y.Uuid != u!.Value)).Take(i!.Value).ToList()),
                    set => set.Uuid,
                    (set, i, c) => Task.FromResult(set.If(i.HasValue, x => x.Any(y => y.Id > i!.Value))),
                    (set, i, c) => Task.FromResult(set.If(i.HasValue, x => x.Any(y => y.Id < i!.Value)))));
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
            Field(o => o.Id);
            Field<ListGraphType<UserTagType>>("userTags", resolve: context => context.Source?.UserTags.ToList());
            Connection<ModelType>()
                .Name("models")
                .Description("Model list")
                .Argument<BooleanGraphType, bool>("nsfw", "Whether or not to include nsfw models in the list. Defaults to false.")
                .Bidirectional()
                .PageSize(100)
                .ResolveAsync(context => context.ResolveConnectionAsync(connectionContext => connectionContext.Source?.Models.Select(t => t.Model).Where(t => t.Nsfw == context.GetArgument<bool>("nsfw"))!,
                    (set, i, u, c) => Task.FromResult(set.OrderByDescending(t => t.Id).If(u.HasValue, x => x.SkipWhile(y => y.Uuid != u!.Value)).TakeLast(i!.Value).ToList()),
                    (set, i, u, c) => Task.FromResult(set.OrderBy(t => t.Id).If(u.HasValue, x => x.SkipWhile(y => y.Uuid != u!.Value)).Take(i!.Value).ToList()),
                    set => set.Uuid,
                    (set, i, c) => Task.FromResult(set.If(i.HasValue, x => x.Any(y => y.Id > i!.Value))),
                    (set, i, c) => Task.FromResult(set.If(i.HasValue, x => x.Any(y => y.Id < i!.Value)))));
        }
    }

    public sealed class UserTagType : ObjectGraphType<UserTags>
    {
        public UserTagType()
        {
            Field(o => o.Name, type: typeof(StringGraphType));
        }
    }

    public sealed class VoteType : ObjectGraphType<Vote>
    {
        public VoteType()
        {
            Field(o => o.DownVote);
            Field(o => o.GameId, type: typeof(StringGraphType));
            Field(o => o.UserId, type: typeof(UIntGraphType));
        }
    }

    public sealed class VoteCompoundType : ObjectGraphType<ModelVoteCondensed>
    {
        public VoteCompoundType()
        {
            Field(o => o.Down, type: typeof(BooleanGraphType));
            Field(o => o.Count, type: typeof(IntGraphType));
        }
    }

    public class ModelVoteCondensed
    {
        public bool Down { get; set; }
        public int Count { get; set; }
    }

    public static class GQLPaginationExtension
    {
        public static async Task<object?> ResolveConnectionAsync<TR, TU, TC>(this IResolveConnectionContext<TC> context, 
            Func<IResolveConnectionContext<TC>, IEnumerable<TR>> func, 
            Func<IEnumerable<TR>, int?, TU?, CancellationToken, Task<List<TR>>> beforeFunc, 
            Func<IEnumerable<TR>, int?, TU?, CancellationToken, Task<List<TR>>> afterFunc, 
            Func<TR, TU> cursorFunc, 
            Func<IEnumerable<TR>, uint?, CancellationToken, Task<bool>> afterCheckFunc,
            Func<IEnumerable<TR>, uint?, CancellationToken, Task<bool>> beforeCheckFunc) where TC : class where TU : struct where TR : BaseId
        {
            var first = context.First;
            var afterCursor = Cursor.FromCursor<TU>(context.After);
            var last = context.Last;
            var beforeCursor = Cursor.FromCursor<TU>(context.Before);
            var cancellationToken = context.CancellationToken;

            var list = await GetListAsync(context, first, afterCursor, last, beforeCursor, cancellationToken, func, beforeFunc, afterFunc).ConfigureAwait(false);
            var getNextPageTask = GetNextPageAsync(context, cancellationToken, func, list, afterCheckFunc);
            var getPreviousPageTask = GetPreviousPageAsync(context, cancellationToken, func, list, beforeCheckFunc);
            var totalCountTask = Task.FromResult(func(context).Count());

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
        private static Task<bool> GetPreviousPageAsync<TR, TC>(IResolveConnectionContext<TC> dbContext, 
            CancellationToken cancellationToken, 
            Func<IResolveConnectionContext<TC>, IEnumerable<TR>> modelFunc,
            List<TR> list,
            Func<IEnumerable<TR>, uint?, CancellationToken, Task<bool>> func) where TR : BaseId
            => func(modelFunc(dbContext), list.FirstOrDefault()?.Id, cancellationToken);

        private static Task<bool> GetNextPageAsync<TR, TC>(IResolveConnectionContext<TC> dbContext, 
            CancellationToken cancellationToken,
            Func<IResolveConnectionContext<TC>, IEnumerable<TR>> modelFunc,
            List<TR> list,
            Func<IEnumerable<TR>, uint?, CancellationToken, Task<bool>> func) where TR : BaseId
            => func(modelFunc(dbContext), list.LastOrDefault()?.Id, cancellationToken);

        private static Task<List<TR>> GetListAsync<TR, TU, TC>(IResolveConnectionContext<TC> dbContext, 
            int? first, 
            TU? afterCursor, 
            int? last, 
            TU? beforeCursor, 
            CancellationToken cancellationToken, 
            Func<IResolveConnectionContext<TC>, IEnumerable<TR>> dbFunc, 
            Func<IEnumerable<TR>, int?, TU?, CancellationToken, Task<List<TR>>> beforeFunc, 
            Func<IEnumerable<TR>, int?, TU?, CancellationToken, Task<List<TR>>> afterFunc) where TR : BaseId
            => first.HasValue ?
                beforeFunc(dbFunc(dbContext), first, afterCursor, cancellationToken) :
                afterFunc(dbFunc(dbContext), last, beforeCursor, cancellationToken);

        private static async Task<(T1, T2, T3)> WaitAll<T1, T2, T3>(Task<T1> task1, Task<T2> task2, Task<T3> task3)
        {
            await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);
            return (await task1.ConfigureAwait(false), await task2.ConfigureAwait(false), await task3.ConfigureAwait(false));
        }
    }
}