using System;
using System.Linq;
using GraphQL.Types;
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
            Field<ListGraphType<ModelType>>("ModelList", resolve: context => dbContext.Models.ToList());
        }
    }
}