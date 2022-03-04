using System;
using Microsoft.Extensions.DependencyInjection;
using ModelSaber.Database;

namespace ModelSaber.API.GraphQL
{
    public class ModelSaberDbContextLeaser
    {
        private readonly IServiceProvider _provider;

        public ModelSaberDbContextLeaser(IServiceProvider provider)
        {
            _provider = provider;
        }

        public ModelSaberDbContext GetContext()
        {
            var scope = _provider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<ModelSaberDbContext>();
        }
    }
}
