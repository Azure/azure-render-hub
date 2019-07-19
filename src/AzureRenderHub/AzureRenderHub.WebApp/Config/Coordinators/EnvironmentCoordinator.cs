// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApp.Code.Contract;

namespace WebApp.Config.Coordinators
{
    public class EnvironmentCoordinator : IEnvironmentCoordinator
    {
        private readonly IConfigRepository<RenderingEnvironment> _inner;

        public EnvironmentCoordinator(IConfigRepository<RenderingEnvironment> inner)
            => _inner = inner;

        public Task<RenderingEnvironment> GetEnvironment(string envId)
            => _inner.Get(envId);

        public Task<List<string>> ListEnvironments()
            => _inner.List();

        public Task<bool> RemoveEnvironment(RenderingEnvironment environment)
            => _inner.Remove(environment.Name);

        public Task UpdateEnvironment(RenderingEnvironment environment, string originalName = null)
            => _inner.Update(environment, environment.Name, originalName);
    }
}
