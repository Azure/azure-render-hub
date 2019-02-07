// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using WebApp.Code.Contract;
using WebApp.Config.Storage;
using WebApp.Models.Storage.Create;

namespace WebApp.Config.Coordinators
{
    public class AssetRepoCoordinator : IAssetRepoCoordinator
    {
        private readonly IGenericConfigCoordinator _configCoordinator;
        private readonly CloudBlobContainer _container;

        public AssetRepoCoordinator(IGenericConfigCoordinator configCoordinator, CloudBlobContainer container)
        {
            _configCoordinator = configCoordinator;
            _container = container;
        }

        public async Task<List<string>> ListRepositories()
        {
            return await _configCoordinator.List(_container);
        }

        public async Task<AssetRepository> GetRepository(string repoName)
        {
            return await _configCoordinator.Get<AssetRepository>(_container, repoName);
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
            await _configCoordinator.Update(_container, repository, repository.Name, originalName);
        }

        public async Task<bool> RemoveRepository(AssetRepository repository)
        {
            return await _configCoordinator.Remove(_container, repository.Name);
        }
    }
}
