using System;
using System.Linq;
using GraphQL.Types;
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
            Field<ListGraphType<ModelType>>("ModelList", "The entire model list endpoint", null, context => dbContext.Models.Include(t => t.Tags).ThenInclude(t => t.Tag));
            Field<ListGraphType<TagType>>("TagList", "The entire tag list endpoint", null, context => dbContext.Tags.Include(t => t.ModelTags).ThenInclude(t => t.Model));
        }
    }
}