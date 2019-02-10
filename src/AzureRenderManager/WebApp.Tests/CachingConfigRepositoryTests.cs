using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using WebApp.Code.Contract;
using WebApp.Config;
using WebApp.Config.Coordinators;
using Xunit;

namespace WebApp.Tests
{
    public class CachingConfigRepositoryTests
    {
        class TestEnvironmentCoordinator : IConfigRepository<RenderingEnvironment>
        {
            private readonly Dictionary<string, RenderingEnvironment> _map = new Dictionary<string, RenderingEnvironment>();

            public Task<RenderingEnvironment> Get(string envId)
            {
                _map.TryGetValue(envId, out var result);
                return Task.FromResult(result);
            }

            public Task<List<string>> List()
            {
                return Task.FromResult(_map.Keys.ToList());
            }

            public Task<bool> Remove(string name)
            {
                return Task.FromResult(_map.Remove(name));
            }

            public Task Update(RenderingEnvironment environment, string newName, string originalName)
            {
                _map[newName] = environment;
                if (originalName != null && originalName != newName)
                {
                    _map.Remove(originalName);
                }

                return Task.CompletedTask;
            }
        }

        private static IMemoryCache NewCache() => new MemoryCache(new MemoryCacheOptions());
        private static RenderingEnvironment NewEnv() => new RenderingEnvironment { Name = Guid.NewGuid().ToString() };

        [Fact]
        public async Task CachesOnGet()
        {
            var inner = new TestEnvironmentCoordinator();
            var outer = new CachingConfigRepository<RenderingEnvironment>(inner, NewCache());

            var env = NewEnv();

            // load cache
            await inner.Update(env, env.Name, null);
            await outer.Get(env.Name);

            // remove real value
            await inner.Remove(env.Name);

            // check it's still cached
            var result = await outer.Get(env.Name);
            Assert.Equal(env, result);
        }

        [Fact]
        public async Task CachesToListOnGet()
        {
            var inner = new TestEnvironmentCoordinator();
            var outer = new CachingConfigRepository<RenderingEnvironment>(inner, NewCache());

            var env = NewEnv();

            await outer.List(); // load list cache

            // load inner then get it 
            await inner.Update(env, env.Name, null);
            await outer.Get(env.Name);

            var result = await outer.List();
            Assert.Equal(env.Name, result.Single());
        }

        [Fact]
        public async Task OverallListUpdatedWhenAdded()
        {
            var inner = new TestEnvironmentCoordinator();
            var outer = new CachingConfigRepository<RenderingEnvironment>(inner, NewCache());

            // load list cache
            await outer.List();

            var env = NewEnv();
            await outer.Update(env, env.Name, null);

            // remove from inner
            await inner.Remove(env.Name);

            // make sure list is cached
            var result = await outer.List();
            Assert.Equal(env.Name, result.Single());
        }

        [Fact]
        public async Task EnvironmentUncachedWhenRemoved()
        {
            var inner = new TestEnvironmentCoordinator();
            var outer = new CachingConfigRepository<RenderingEnvironment>(inner, NewCache());

            var env = NewEnv();
            await outer.Update(env, env.Name, null);

            await outer.Remove(env.Name);

            Assert.Null(await outer.Get(env.Name));
        }

        [Fact]
        public async Task EnvironmentListUncachedWhenRemoved()
        {
            // want to make sure this works no matter where the cache is first loaded
            for (int i = 0; i < 3; ++i)
            {
                var inner = new TestEnvironmentCoordinator();
                var outer = new CachingConfigRepository<RenderingEnvironment>(inner, NewCache());

                if (i == 0) await outer.List();

                var env = NewEnv();
                await outer.Update(env, env.Name, null);

                if (i == 1) await outer.List();

                await outer.Remove(env.Name);

                Assert.Empty(await outer.List());
            }
        }

        [Fact]
        public async Task OriginalEnvironmentUncachedWhenRenamed()
        {
            var inner = new TestEnvironmentCoordinator();
            var outer = new CachingConfigRepository<RenderingEnvironment>(inner, NewCache());

            var env = NewEnv();
            await outer.Update(env, env.Name, null);

            var renamedEnv = NewEnv();
            await outer.Update(renamedEnv, renamedEnv.Name, env.Name);

            var result = await outer.Get(env.Name);
            Assert.Null(result);
        }

        [Fact]
        public async Task OverallListUpdatedWhenRenamed()
        {
            // want to make sure this works no matter when list cache is loaded
            for (int i = 0; i < 3; ++i)
            {
                var inner = new TestEnvironmentCoordinator();
                var outer = new CachingConfigRepository<RenderingEnvironment>(inner, NewCache());

                if (i == 0) await outer.List();
                
                var env = NewEnv();
                await outer.Update(env, env.Name, null);

                if (i == 1) await outer.List();

                var renamedEnv = NewEnv();
                await outer.Update(renamedEnv, renamedEnv.Name, env.Name);

                var result = await outer.List();
                Assert.Equal(renamedEnv.Name, result.Single());
            }
        }
    }
}
