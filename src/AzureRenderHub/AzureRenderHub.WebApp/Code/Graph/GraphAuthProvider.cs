using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using WebApp.Code.Session;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Security.Claims;
using WebApp.Code.Extensions;

namespace WebApp.Code.Graph
{
    public class GraphAuthProvider : IGraphAuthProvider
    {
        private readonly IMemoryCache _memoryCache;
        private readonly AzureAdOptions _adOptions = new AzureAdOptions();
        private readonly ClientCredential _credential;
        private readonly string[] _scopes;

        public GraphAuthProvider(IMemoryCache memoryCache, IConfiguration configuration)
        {
            configuration.Bind("AzureAd", _adOptions);
            _credential = new ClientCredential(_adOptions.ClientId, _adOptions.ClientSecret);
            _scopes = _adOptions.GraphScopes.Split(new[] { ' ' });
            _memoryCache = memoryCache;
        }

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
                    _adOptions.GraphResourceId,
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
            return new AuthenticationContext(
                _adOptions.Authority,
                true);
        }
    }

    public interface IGraphAuthProvider
    {
        Task<string> GetUserAccessTokenAsync(ClaimsPrincipal user);
    }
}
