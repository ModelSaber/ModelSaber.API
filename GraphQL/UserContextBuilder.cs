using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GraphQL.Server.Transports.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ModelSaber.API.Helpers;
using ModelSaber.Database;

namespace ModelSaber.API.GraphQL
{
    public class UserContextBuilder : IUserContextBuilder
    {
        private readonly IServiceProvider _provider;
        private readonly AppSettings _appSettings;

        public UserContextBuilder(IServiceProvider provider, IOptions<AppSettings> appSettings)
        {
            _provider = provider;
            _appSettings = appSettings.Value;
        }

        public Task<IDictionary<string, object>> BuildUserContext(HttpContext httpContext)
        {
            var ret = new Dictionary<string, object?> { { "auth", null } };

            var auth = httpContext.Request.Headers.Authorization.ToString();

            // ReSharper disable once InvertIf
            if (!string.IsNullOrWhiteSpace(auth))
            {
                using var dbContext = _provider.CreateScope().ServiceProvider.GetRequiredService<ModelSaberDbContext>();
                var index = auth.IndexOf(" ", StringComparison.Ordinal) + 1;
                var type = auth[..(index - 1)];
                if (type == "Bearer")
                {
                    var token = Convert.FromBase64String(auth[index..]);
                    var tokens = dbContext.OAuthTokens.ToList();
                    var dbToken = tokens.FirstOrDefault(t => t.Token.SequenceEqual(token));
                    ret["auth"] = dbToken;
                }
                else
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.UTF8.GetBytes(_appSettings.Secret);
                    tokenHandler.ValidateToken(auth[index..], new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero
                    }, out var validatedToken);
                    var jwtToken = (JwtSecurityToken) validatedToken;
                    var guidString = jwtToken.Claims.First(x => x.Type == "guid").Value;
                    Guid.TryParse(guidString, out var guid);
                    ret["auth"] = dbContext.Logons.FirstOrDefault(t => t.Id == guid);
                }
            }

            return Task.FromResult(ret as IDictionary<string, object>);
        }
    }
}
