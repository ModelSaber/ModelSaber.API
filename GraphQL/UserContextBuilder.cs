using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Server.Transports.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelSaber.Database;

namespace ModelSaber.API.GraphQL
{
    public class UserContextBuilder : IUserContextBuilder
    {
        private readonly IServiceProvider _provider;

        public UserContextBuilder(IServiceProvider provider)
        {
            _provider = provider;
        }

        public Task<IDictionary<string, object>> BuildUserContext(HttpContext httpContext)
        {
            var ret = new Dictionary<string, object?> { { "auth", null } };

            var auth = httpContext.Request.Headers.Authorization.ToString();

            // ReSharper disable once InvertIf
            if (!string.IsNullOrWhiteSpace(auth))
            {
                using var dbContext = _provider.CreateScope().ServiceProvider.GetService<ModelSaberDbContext>();
                var index = auth.IndexOf(" ", StringComparison.Ordinal) + 1;
                var token = Convert.FromBase64String(auth.Remove(0, index));
                var tokens = dbContext.OAuthTokens.ToList();
                var dbToken = tokens.FirstOrDefault(t => t.Token.SequenceEqual(token));
                ret["auth"] = dbToken;
            }

            return Task.FromResult(ret as IDictionary<string, object>);
        }
    }
}
