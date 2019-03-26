using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using System.Linq;
using WebApp.Code.Session;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Security.Claims;
using WebApp.Code.Extensions;

namespace WebApp.Code.Graph
{
    public class GraphAuthProvider : IGraphAuthProvider
    {
        private readonly AzureAdConfig _azureAdConfig;
        private readonly IMemoryCache _memoryCache;
        private TokenCache _userTokenCache;

        // Properties used to get and manage an access token.
        private readonly string _appId;
        private readonly ClientCredential _credential;
        private readonly string[] _scopes;
        private readonly string _redirectUri;
        private readonly string _authEndpointPrefix;
        private readonly string _authority;
        private readonly string _resourceId;

        public GraphAuthProvider(IMemoryCache memoryCache, IConfiguration configuration)
        {
            _appId = configuration["AzureAd:ClientId"];
            _credential = new ClientCredential(configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"]);
            _scopes = configuration["AzureAd:GraphScopes"].Split(new[] { ' ' });
            _redirectUri = configuration["AzureAd:BaseUrl"] + configuration["AzureAd:CallbackPath"];
            _authEndpointPrefix = configuration["AzureAd:AuthEndpointPrefix"];
            _authority = configuration["AzureAd:Instance"] + configuration["AzureAd:TenantId"];
            _resourceId = configuration["AzureAd:GraphResourceId"];
            _memoryCache = memoryCache;
        }

        // Gets an access token. First tries to get the access token from the token cache.
        // Using password (secret) to authenticate. Production apps should use a certificate.
        public async Task<string> GetUserAccessTokenAsync(ClaimsPrincipal user)
        {
            var objectId = user.Claims.GetObjectId();
            var issuerValue = user.GetIssuerValue();
            var userName = user.Identity?.Name;

            try
            {
                var authContext = await CreateAuthenticationContext(user)
                    .ConfigureAwait(false);
                var userId = new UserIdentifier(objectId, UserIdentifierType.UniqueId);
                var result = await authContext.AcquireTokenSilentAsync(
                    _resourceId,
                    _credential,
                    userId)
                    .ConfigureAwait(false);

                return result.AccessToken;
            }
            catch (AdalException ex)
            {
                throw new Exception($"AcquireTokenSilentAsync failed for user: {objectId}", ex);
            }
        }

        private async Task<AuthenticationContext> CreateAuthenticationContext(ClaimsPrincipal claimsPrincipal)
        {
            var cache = new SessionTokenCache(claimsPrincipal, _memoryCache);
            var tokenCache = cache.GetCacheInstance();
            return new AuthenticationContext(
                _authority,
                true,
                tokenCache);
        }
    }

    public interface IGraphAuthProvider
    {
        Task<string> GetUserAccessTokenAsync(ClaimsPrincipal user);
    }
}
