using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Security.Claims;
using System.Text;
using WebApp.Code.Extensions;

namespace WebApp.Code.Session
{
    public class SessionTokenCache
    {
        private readonly string _cacheKey;
        private ClaimsPrincipal _claimsPrincipal;
        private readonly IMemoryCache _memoryCache;
        private TokenCache _cache = new TokenCache();

        public SessionTokenCache(ClaimsPrincipal claimsPrincipal, IMemoryCache memoryCache)
        {
            _cacheKey = BuildCacheKey(claimsPrincipal);
            _claimsPrincipal = claimsPrincipal;
            _memoryCache = memoryCache;
            Load();
        }

        private static string BuildCacheKey(ClaimsPrincipal claimsPrincipal)
        {
            // TODO: This needs to be updated in a multi-tenant env.
            return $"UserId:{claimsPrincipal.Claims.GetObjectId()}";
        }

        public TokenCache GetCacheInstance()
        {
            _cache.BeforeAccess = BeforeAccessNotification;
            _cache.AfterAccess = AfterAccessNotification;
            Load();
            return _cache;
        }

        public void Load()
        {
            _cache.Deserialize(_memoryCache.Get(_cacheKey) as byte[]);
        }

        public void Persist()
        {
            _memoryCache.Set(_cacheKey, _cache.Serialize());
            _cache.HasStateChanged = false;
        }
        
        public void Clear()
        {
            _cache = null;
            _memoryCache.Remove(_cacheKey);
        }

        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load();
        }

        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (_cache.HasStateChanged)
            {
                Persist();
            }
        }
    }
}
