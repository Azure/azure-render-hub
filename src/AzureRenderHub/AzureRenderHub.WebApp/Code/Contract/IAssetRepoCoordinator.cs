// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using AzureRenderHub.WebApp.Arm.Deploying;
using AzureRenderHub.WebApp.Code.Contract;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApp.Config.Storage;
using WebApp.Models.Storage.Create;

namespace WebApp.Code.Contract
{
    public interface IAssetRepoCoordinator
    {
        Task<List<string>> ListRepositories();

        Task<AssetRepository> GetRepository(string repoName);

        AssetRepository CreateRepository(AddAssetRepoBaseModel model);

        Task BeginRepositoryDeploymentAsync(AssetRepository repository);

        Task UpdateRepositoryFromDeploymentAsync(AssetRepository repository);

        Task BeginDeleteRepositoryAsync(AssetRepository repository, bool deleteResourceGroup);

        Task DeleteRepositoryResourcesAsync(AssetRepository repository, bool deleteResourceGroup);

        Task UpdateRepository(AssetRepository repository, string originalName = null);

        Task<bool> RemoveRepository(AssetRepository repository);

        Task<List<VirtualMachineStatus>> GetVirtualMachineStatus(AssetRepository repository);
    }
}