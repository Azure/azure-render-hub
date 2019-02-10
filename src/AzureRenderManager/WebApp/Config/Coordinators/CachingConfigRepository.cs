// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Config.Coordinators
{
    public class CachingConfigRepository<T> : IConfigRepository<T>
    {
        private readonly IConfigRepository<T> _inner;
        private readonly IMemoryCache _cache;

        public CachingConfigRepository(IConfigRepository<T> inner, IMemoryCache cache)
        {
            _inner = inner;
            _cache = cache;
        }

        private static readonly string TypeName = typeof(T).Name.ToUpperInvariant();
        private static readonly string AllEnvironmentsKey = TypeName + "_ALL";

        private static readonly TimeSpan CacheListFor = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan CacheEntryFor = TimeSpan.FromMinutes(15);

        private static string CacheKey(string name)
            => TypeName + ":" + name;

        // Note: we're using this as a concurrent hashset, the values don't matter
        private ConcurrentDictionary<string, byte> GetCachedList()
            => _cache.Get<ConcurrentDictionary<string, byte>>(AllEnvironmentsKey);

        public Task<T> Get(string name)
            => _cache.GetOrCreateAsync(CacheKey(name), async cacheEntry =>
            {
                var result = await _inner.Get(name);

                // add to cached list
                var all = GetCachedList();
                if (all != null)
                {
                    all[name] = 0;
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
            var cachedResult = await _cache.GetOrCreateAsync(AllEnvironmentsKey, async cacheEntry =>
            {
                var result = await _inner.List();
                var cacheableResult = new ConcurrentDictionary<string, byte>(result.Select(x => new KeyValuePair<string, byte>(x, 0)));

                cacheEntry.SetAbsoluteExpiration(CacheListFor);

                return cacheableResult;
            });

            return cachedResult.Keys.ToList();
        }

        public Task<bool> Remove(string name)
        {
            var result = _inner.Remove(name);

            // remove from cached list
            var allEnvironments = GetCachedList();
            if (allEnvironments != null)
            {
                allEnvironments.Remove(name, out _);
            }

            // remove from entry cache
            _cache.Remove(CacheKey(name));

            return result;
        }

        public async Task Update(T entity, string newName, string originalName)
        {
            await _inner.Update(entity, originalName);

            // overwrite cached value
            _cache.Set(CacheKey(newName), entity, CacheEntryFor);

            if (originalName != null && originalName != newName)
            {
                // if renamed, remove cached value from list and entry cache, and add new one
                var all = GetCachedList();
                if (all != null)
                {
                    all.Remove(originalName, out _);
                    all[newName] = 0;
                }

                _cache.Remove(CacheKey(originalName));
            }
            else
            {
                var allEntities = GetCachedList();
                if (allEntities != null)
                {
                    // otherwise make sure it is in the list
                    allEntities[newName] = 0;
                }
            }
        }
    }
}
