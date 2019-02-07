// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace WebApp.Config.Coordinators
{
    public class CachingConfigCoordinator : IGenericConfigCoordinator
    {
        private readonly IGenericConfigCoordinator _inner;
        private readonly IMemoryCache _cache;
        private readonly string _moniker;
        private readonly string _allConfigKey;

        public CachingConfigCoordinator(IGenericConfigCoordinator inner, IMemoryCache cache, string moniker)
        {
            _inner = inner;
            _cache = cache;
            _moniker = moniker;
            _allConfigKey = $"CONFIGS_ALL:{moniker}";
        }

        private static readonly TimeSpan CacheListFor = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan CacheEntryFor = TimeSpan.FromMinutes(15);

        private string CacheKey(string configName) => $"CONFIG:{_moniker}:{configName}";

        // Note: we're using this as a concurrent hashset, the values don't matter
        private ConcurrentDictionary<string, byte> GetCachedConfigList()
            => _cache.Get<ConcurrentDictionary<string, byte>>(_allConfigKey);

        public Task<T> Get<T>(string configName)
            => _cache.GetOrCreateAsync(CacheKey(configName), async cacheEntry =>
            {
                var result = await _inner.Get<T>(configName);

                // add to config list cache
                var allConfigs = GetCachedConfigList();
                if (allConfigs != null)
                {
                    allConfigs[configName] = 0;
                }
                else
                {
                    // if not already loaded then don't bother as we need to do a full enumeration anyway, so defer it
                }

                cacheEntry.SetAbsoluteExpiration(CacheEntryFor);

                return result;
            });

        public async Task<List<string>> List()
        {
            var cachedResult = await _cache.GetOrCreateAsync(_allConfigKey, async cacheEntry =>
            {
                var result = await _inner.List();
                var cacheableResult = new ConcurrentDictionary<string, byte>(result.Select(x => new KeyValuePair<string, byte>(x, 0)));

                cacheEntry.SetAbsoluteExpiration(CacheListFor);

                return cacheableResult;
            });

            return cachedResult.Keys.ToList();
        }

        public Task<bool> Remove(string configName)
        {
            var result = _inner.Remove(configName);

            // remove from config list cache
            var allConfigs = GetCachedConfigList();
            if (allConfigs != null)
            {
                allConfigs.Remove(configName, out _);
            }

            // remove from entry cache
            _cache.Remove(CacheKey(configName));

            return result;
        }

        public async Task Update<T>(T config, string configName, string originalName = null)
        {
            await _inner.Update(config, originalName);

            // overwrite cached value
            _cache.Set(CacheKey(configName), config, CacheEntryFor);

            if (originalName != null && originalName != configName)
            {
                // if renamed, remove cached value from list and entry cache, and add new one
                var allConfigs = GetCachedConfigList();
                if (allConfigs != null)
                {
                    allConfigs.Remove(originalName, out _);
                    allConfigs[configName] = 0;
                }

                _cache.Remove(CacheKey(originalName));
            }
            else
            {
                var allConfigs = GetCachedConfigList();
                if (allConfigs != null)
                {
                    // otherwise make sure it is in the list
                    allConfigs[configName] = 0;
                }
            }
        }
    }
}
