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
    public class CachingEnvironmentCoordinatorTests
    {
        class TestEnvironmentCoordinator : IEnvironmentCoordinator
        {
            private readonly Dictionary<string, RenderingEnvironment> _map = new Dictionary<string, RenderingEnvironment>();

            public Task<RenderingEnvironment> GetEnvironment(string envId)
            {
                _map.TryGetValue(envId, out var result);
                return Task.FromResult(result);
            }

            public Task<List<string>> ListEnvironments()
            {
                return Task.FromResult(_map.Keys.ToList());
            }

            public Task<bool> RemoveEnvironment(RenderingEnvironment environment)
            {
                return Task.FromResult(_map.Remove(environment.Name));
            }

            public Task UpdateEnvironment(RenderingEnvironment environment, string originalName = null)
            {
                _map[environment.Name] = environment;
                if (originalName != null && originalName != environment.Name)
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
            var outer = new CachingEnvironmentCoordinator(inner, NewCache());

            var env = NewEnv();

            // load cache
            await inner.UpdateEnvironment(env);
            await outer.GetEnvironment(env.Name);

            // remove real value
            await inner.RemoveEnvironment(env);

            // check it's still cached
            var result = await outer.GetEnvironment(env.Name);
            Assert.Equal(env, result);
        }

        [Fact]
        public async Task CachesToListOnGet()
        {
            var inner = new TestEnvironmentCoordinator();
            var outer = new CachingEnvironmentCoordinator(inner, NewCache());

            var env = NewEnv();

            await outer.ListEnvironments(); // load list cache

            // load inner then get it 
            await inner.UpdateEnvironment(env);
            await outer.GetEnvironment(env.Name);

            var result = await outer.ListEnvironments();
            Assert.Equal(env.Name, result.Single());
        }

        [Fact]
        public async Task OverallListUpdatedWhenAdded()
        {
            var inner = new TestEnvironmentCoordinator();
            var outer = new CachingEnvironmentCoordinator(inner, NewCache());

            // load list cache
            await outer.ListEnvironments();

            var env = NewEnv();
            await outer.UpdateEnvironment(env);

            // remove from inner
            await inner.RemoveEnvironment(env);

            // make sure list is cached
            var result = await outer.ListEnvironments();
            Assert.Equal(env.Name, result.Single());
        }

        [Fact]
        public async Task EnvironmentUncachedWhenRemoved()
        {
            var inner = new TestEnvironmentCoordinator();
            var outer = new CachingEnvironmentCoordinator(inner, NewCache());

            var env = NewEnv();
            await outer.UpdateEnvironment(env);

            await outer.RemoveEnvironment(env);

            Assert.Null(await outer.GetEnvironment(env.Name));
        }

        [Fact]
        public async Task EnvironmentListUncachedWhenRemoved()
        {
            // want to make sure this works no matter where the cache is first loaded
            for (int i = 0; i < 3; ++i)
            {
                var inner = new TestEnvironmentCoordinator();
                var outer = new CachingEnvironmentCoordinator(inner, NewCache());

                if (i == 0) await outer.ListEnvironments();

                var env = NewEnv();
                await outer.UpdateEnvironment(env);

                if (i == 1) await outer.ListEnvironments();

                await outer.RemoveEnvironment(env);

                Assert.Empty(await outer.ListEnvironments());
            }
        }

        [Fact]
        public async Task OriginalEnvironmentUncachedWhenRenamed()
        {
            var inner = new TestEnvironmentCoordinator();
            var outer = new CachingEnvironmentCoordinator(inner, NewCache());

            var env = NewEnv();
            await outer.UpdateEnvironment(env);

            var renamedEnv = NewEnv();
            await outer.UpdateEnvironment(renamedEnv, env.Name);

            var result = await outer.GetEnvironment(env.Name);
            Assert.Null(result);
        }

        [Fact]
        public async Task OverallListUpdatedWhenRenamed()
        {
            // want to make sure this works no matter when list cache is loaded
            for (int i = 0; i < 3; ++i)
            {
                var inner = new TestEnvironmentCoordinator();
                var outer = new CachingEnvironmentCoordinator(inner, NewCache());

                if (i == 0) await outer.ListEnvironments();
                
                var env = NewEnv();
                await outer.UpdateEnvironment(env);

                if (i == 1) await outer.ListEnvironments();

                var renamedEnv = NewEnv();
                await outer.UpdateEnvironment(renamedEnv, env.Name);

                var result = await outer.ListEnvironments();
                Assert.Equal(renamedEnv.Name, result.Single());
            }
        }
    }
}
