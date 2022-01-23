using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Server.Transports.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace ModelSaber.API.GraphQL
{
    public class UserContextBuilder : IUserContextBuilder
    {
        public Task<IDictionary<string, object>> BuildUserContext(HttpContext httpContext)
        {
            var ret = new Dictionary<string, object>();

            var auth = httpContext.Request.Headers.Authorization.ToString();

            ret.Add("auth", auth);
            
            return Task.FromResult(ret as IDictionary<string, object>);
        }
    }
}
