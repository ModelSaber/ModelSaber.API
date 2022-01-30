using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModelSaber.Database;
using ModelSaber.Models;
using Newtonsoft.Json;

namespace ModelSaber.API.OAuth
{
    [Route("oauth")]
    [ApiController]
    public class OAuthController : ControllerBase
    {
        private readonly ModelSaberDbContext _dbContext;

        public OAuthController(ModelSaberDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("/token")]
        public IActionResult Token([FromForm] OAuthTokenRequest request)
        {
            if (!OAuthTokenResponse.ValidateParameters(request, out var missingParam)) return BadRequest(OAuthTokenResponse.MissingParamRequest(missingParam));
            var clientId = Guid.Parse(request.ClientId);
            switch (request.AuthorizationCode)
            {
                case "client_credentials":
                    var client = _dbContext.OAuthClients.SingleOrDefault(e => e.ClientId == clientId);
                    if (client == null)
                        return BadRequest(OAuthTokenResponse.InvalidClient("Invalid client_id"));
                    if (client.ClientSecret != request.ClientSecret)
                        return BadRequest(OAuthTokenResponse.InvalidClient("Invalid client_secret"));
                    var token = client.GetToken();
                    var tokens = _dbContext.OAuthTokens.ToList();
                    while (tokens.Any(t => t.Token.SequenceEqual(token.Token)))
                        token = client.GetToken();
                    _dbContext.OAuthTokens.Add(token);
                    _dbContext.SaveChanges();
                    return Ok(OAuthTokenResponse.CreateSuccessResponse(token));
                case "authorization_code":
                case "password":
                    return BadRequest(OAuthTokenResponse.UnsupportedGrantType(request.AuthorizationCode));
                default:
                    return BadRequest(OAuthTokenResponse.InvalidGrant());
            }
        }
    }

    public class OAuthTokenResponse
    {
        public static bool ValidateParameters(OAuthTokenRequest request, out string missingParam)
        {
            missingParam = "";
            if (string.IsNullOrWhiteSpace(request.AuthorizationCode))
            {
                missingParam = "authorization_code";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.ClientId))
            {
                missingParam = "client_id";
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.ClientSecret))
            {
                missingParam = "client_secret";
                return false;
            }
            return true;
        }

        public static OAuthSuccessResponse CreateSuccessResponse(OAuthToken token)
        {
            var refreshToken = token.Refresh.Length > 0 ? Convert.ToBase64String(token.Refresh) : null;
            return new OAuthSuccessResponse(Convert.ToBase64String(token.Token), "Bearer", refreshToken, token.Scope, token.ExpiresIn);
        }

        #region OAuthErrorResponse
        public static OAuthErrorResponse MissingParamRequest(string missingParam) => MakeErrorResponse("invalid_request", $"The request is missing a required parameter on the server. Param name: ${missingParam}");
        public static OAuthErrorResponse InvalidClient(string? error = null) => MakeErrorResponse("invalid_client", error);
        public static OAuthErrorResponse InvalidGrant() => MakeErrorResponse("invalid_grant");
        public static OAuthErrorResponse UnauthorizedClient() => MakeErrorResponse("unauthorized_client");
        public static OAuthErrorResponse UnsupportedGrantType(string grantType) => MakeErrorResponse("unsupported_grant_type", $"The grant type ${grantType} is not currently supported on this server.");
        public static OAuthErrorResponse InvalidScope() => MakeErrorResponse("invalid_scope");
        public static OAuthErrorResponse MakeErrorResponse(string error, string? description = null) => new (error, description);

        #endregion
    }

    #region Structs
    public class OAuthTokenRequest
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }
        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }
        [JsonProperty("authorization_code")]
        public string AuthorizationCode { get; set; }
        [JsonProperty("scope")]
        public string Scope { get; set; }
    }

    public struct OAuthSuccessResponse
    {
        public OAuthSuccessResponse(string accessToken, string tokenType, string? refreshToken, string? scope, uint expiresIn)
        {
            AccessToken = accessToken;
            TokenType = tokenType;
            RefreshToken = refreshToken;
            Scope = scope;
            ExpiresIn = expiresIn;
        }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty("token_type")]
        public string TokenType { get; set; }
        [JsonProperty("expires_in")]
        public uint ExpiresIn { get; set; }
        [JsonProperty("refresh_token")]
        public string? RefreshToken { get; set; }
        [JsonProperty("scope")]
        public string? Scope { get; set; }
    }

    public struct OAuthErrorResponse
    {
        public OAuthErrorResponse(string error, string? description)
        {
            Error = error;
            ErrorDescription = description;
        }

        [JsonProperty("error")]
        public string Error { get; set; }
        [JsonProperty("error_description")]
        public string? ErrorDescription { get; set; }
    }
    #endregion
}
