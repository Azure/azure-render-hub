// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Threading.Tasks;

using WebApp.Config.Storage;
using WebApp.Models.Storage.Create;

namespace WebApp.Code.Contract
{
    public interface IAssetRepoCoordinator
    {
        Task<List<AssetRepository>> GetRepositories();

        Task<AssetRepository> GetRepository(string repoName);

        AssetRepository CreateRepository(AddAssetRepoBaseModel model);

        Task UpdateRepository(AssetRepository repository, string originalName = null);

        Task UpdateRepositories(IEnumerable<AssetRepository> repositories);

        Task<bool> RemoveRepository(AssetRepository repository);
    }
}