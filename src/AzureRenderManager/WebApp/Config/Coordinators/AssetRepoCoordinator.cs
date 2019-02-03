// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using WebApp.Code;
using WebApp.Code.Contract;
using WebApp.Code.Extensions;
using WebApp.Config.Storage;
using WebApp.Models.Storage.Create;

namespace WebApp.Config.Coordinators
{
    public class AssetRepoCoordinator : IAssetRepoCoordinator
    {
        private readonly IPortalConfigurationProvider _portalConfigurationProvider;

        public AssetRepoCoordinator(IPortalConfigurationProvider portalConfigurationProvider)
        {
            _portalConfigurationProvider = portalConfigurationProvider;
        }

        public async Task<List<AssetRepository>> GetRepositories()
        {
            var config = await _portalConfigurationProvider.GetConfig();
            var repositories = config != null ? config.Repositories : new List<AssetRepository>();
            return repositories;
        }

        public async Task<AssetRepository> GetRepository(string repoName)
        {
            var repositories = await GetRepositories();
            var found = repositories?.FirstOrDefault(repo => repo.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase));

            return found;
        }

        public AssetRepository CreateRepository(AddAssetRepoBaseModel model)
        {
            switch (model.RepositoryType)
            {
                case AssetRepositoryType.AvereCluster:
                    return new AvereCluster { Name = model.RepositoryName, InProgress = true };

                case AssetRepositoryType.NfsFileServer:
                    return new NfsFileServer { Name = model.RepositoryName, InProgress = true };

                default:
                    throw new NotSupportedException("Unknown type of repository selected");
            }
        }

        public async Task UpdateRepository(AssetRepository repository, string originalName = null)
        {
            var repositories = await GetRepositories() ?? new List<AssetRepository>();
            var index = repositories.FindIndex(env => env.Name.Equals(originalName ?? repository.Name, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                repositories.Add(repository);
            }
            else
            {
                repositories[index] = repository;
            }

            await UpdateRepositories(repositories);
        }

        public async Task UpdateRepositories(IEnumerable<AssetRepository> repositories)
        {
            var config = await _portalConfigurationProvider.GetConfig();
            if (config != null)
            {
                config.Repositories = repositories.ToList();
                await _portalConfigurationProvider.SetConfig(config);
            }
        }

        public async Task<bool> RemoveRepository(AssetRepository repository)
        {
            var repositories = await GetRepositories();
            var index = repositories?.FindIndex(env => env.Name.Equals(repository.Name, StringComparison.OrdinalIgnoreCase));
            if (index.GetValueOrDefault(-1) > -1)
            {
                repositories?.RemoveAt(index.Value);
                await UpdateRepositories(repositories);

                return true;
            }

            return false;
        }
    }
}
