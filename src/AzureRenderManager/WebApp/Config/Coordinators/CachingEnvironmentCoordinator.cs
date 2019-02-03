// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using WebApp.Code.Contract;

namespace WebApp.Config.Coordinators
{
    public class CachingEnvironmentCoordinator : IEnvironmentCoordinator
    {
        private readonly IEnvironmentCoordinator _inner;
        private readonly IMemoryCache _cache;

        public CachingEnvironmentCoordinator(IEnvironmentCoordinator inner, IMemoryCache cache)
        {
            _inner = inner;
            _cache = cache;
        }

        private const string AllEnvironmentsKey = "ENVIRONMENTS_ALL";
        private static readonly TimeSpan CacheListFor = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan CacheEntryFor = TimeSpan.FromMinutes(15);

        private static string CacheKey(string environmentId)
            => "ENVIRONMENT:" + environmentId;

        // Note: we're using this as a concurrent hashset, the values don't matter
        private ConcurrentDictionary<string, byte> GetCachedEnvironmentList()
            => _cache.Get<ConcurrentDictionary<string, byte>>(AllEnvironmentsKey);

        public Task<RenderingEnvironment> GetEnvironment(string envId)
            => _cache.GetOrCreateAsync(CacheKey(envId), async cacheEntry =>
            {
                var result = await _inner.GetEnvironment(envId);

                // add to env list cache
                var allEnvironments = GetCachedEnvironmentList();
                if (allEnvironments != null)
                {
                    allEnvironments[envId] = 0;
                }
                else
                {
                    // if not already loaded then don't bother as we need to do a full enumeration anyway, so defer it
                }

                cacheEntry.SetAbsoluteExpiration(CacheEntryFor);

                return result;
            });

        public async Task<List<string>> ListEnvironments()
        {
            var cachedResult = await _cache.GetOrCreateAsync(AllEnvironmentsKey, async cacheEntry =>
            {
                var result = await _inner.ListEnvironments();
                var cacheableResult = new ConcurrentDictionary<string, byte>(result.Select(x => new KeyValuePair<string, byte>(x, 0)));

                cacheEntry.SetAbsoluteExpiration(CacheListFor);

                return cacheableResult;
            });

            return cachedResult.Keys.ToList();
        }

        public Task<bool> RemoveEnvironment(RenderingEnvironment environment)
        {
            var result = _inner.RemoveEnvironment(environment);

            // remove from env list cache
            var allEnvironments = GetCachedEnvironmentList();
            if (allEnvironments != null)
            {
                allEnvironments.Remove(environment.Name, out _);
            }

            // remove from entry cache
            _cache.Remove(CacheKey(environment.Name));

            return result;
        }

        public async Task UpdateEnvironment(RenderingEnvironment environment, string originalName = null)
        {
            await _inner.UpdateEnvironment(environment, originalName);

            // overwrite cached value
            _cache.Set(CacheKey(environment.Name), environment, CacheEntryFor);

            if (originalName != null && originalName != environment.Name)
            {
                // if renamed, remove cached value from list and entry cache, and add new one
                var allEnvironments = GetCachedEnvironmentList();
                if (allEnvironments != null)
                {
                    allEnvironments.Remove(originalName, out _);
                    allEnvironments[environment.Name] = 0;
                }

                _cache.Remove(CacheKey(originalName));
            }
            else
            {
                var allEnvironments = GetCachedEnvironmentList();
                if (allEnvironments != null)
                {
                    // otherwise make sure it is in the list
                    allEnvironments[environment.Name] = 0;
                }
            }
        }
    }
}
