using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            //Field<ListGraphType<ModelType>>("models", "The entire model list endpoint", null, context => dbContext.Models.Include(t => t.Tags).ThenInclude(t => t.Tag).Include(t => t.Users).ThenInclude(t => t.User));
            Field<ListGraphType<TagType>>("tags", "The entire tag list endpoint", null, context => dbContext.Tags.Include(t => t.ModelTags).ThenInclude(t => t.Model).ThenInclude(t => t.Users).ThenInclude(t => t.User));
            Field<ListGraphType<UserType>>("users", "The entire user list", null, context => dbContext.Users.Include(t => t.Models).ThenInclude(t => t.Model).ThenInclude(t => t.Tags).ThenInclude(t => t.Tag));
            Connection<ModelType>()
                .Name("models")
                .Description("Model list")
                .Bidirectional()
                .PageSize(100)
                .ResolveAsync(context => ResolveModelConnectionAsync(dbContext, context));
        }

        private async Task<object> ResolveModelConnectionAsync(ModelSaberDbContext dbContext, IResolveConnectionContext<object> context)
        {
            var first = context.First;
            var afterCursor = Cursor.FromCursorDateTime(context.After);
            var last = context.Last;
            var beforeCursor = Cursor.FromCursorDateTime(context.Before);
            var cancellationToken = context.CancellationToken;

            var getModelsTask = GetModelAsync(dbContext, first, afterCursor, last, beforeCursor, cancellationToken);
            var getNextPageTask = GetModelsNextPageAsync(dbContext, first, afterCursor, cancellationToken);
            var getPreviousPageTask = GetModelPreviousPageAsync(dbContext, last, beforeCursor, cancellationToken);
            var totalCountTask = Task.FromResult(dbContext.Models.Count());

            await Task.WhenAll(getModelsTask, getNextPageTask, getPreviousPageTask, totalCountTask).ConfigureAwait(false);
            var models = await getModelsTask.ConfigureAwait(false);
            var nextPage = await getNextPageTask.ConfigureAwait(false);
            var previousPage = await getPreviousPageTask.ConfigureAwait(false);
            var totalCount = await totalCountTask.ConfigureAwait(false);
            var (firstCursor, lastCursor) = Cursor.GetFirstAndLastCursor(models, x => x?.Date);

            return new Connection<Model>
            {
                Edges = models.Select(x => new Edge<Model>
                {
                    Cursor = Cursor.ToCursor(x.Date),
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

        private Task<bool> GetModelPreviousPageAsync(ModelSaberDbContext dbContext, int? last, DateTime? beforeCursor, CancellationToken cancellationToken)
            => dbContext.Models.GetModelPreviousPageAsync(last, beforeCursor, cancellationToken);

        private Task<bool> GetModelsNextPageAsync(ModelSaberDbContext dbContext, int? first, DateTime? afterCursor, CancellationToken cancellationToken)
            => dbContext.Models.GetModelNextPageAsync(first, afterCursor, cancellationToken);

        private Task<List<Model>> GetModelAsync(ModelSaberDbContext dbContext, int? first, DateTime? afterCursor, int? last, DateTime? beforeCursor, CancellationToken cancellationToken) 
            => first.HasValue ? dbContext.Models.GetModelAsync(first, afterCursor, cancellationToken) : dbContext.Models.GetModelReverseAsync(last, beforeCursor, cancellationToken);
    }
}