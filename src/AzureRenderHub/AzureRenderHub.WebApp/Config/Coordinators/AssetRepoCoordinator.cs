// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;
using Newtonsoft.Json.Linq;
using WebApp.Arm;
using WebApp.BackgroundHosts.Deployment;
using WebApp.Code.Contract;
using WebApp.Config.Storage;
using WebApp.Identity;
using WebApp.Models.Storage.Create;
using WebApp.Operations;
using WebApp.Providers.Templates;

namespace WebApp.Config.Coordinators
{
    public class AssetRepoCoordinator : IAssetRepoCoordinator
    {
        private readonly IConfigRepository<AssetRepository> _configCoordinator;
        private readonly ITemplateProvider _templateProvider;
        private readonly IIdentityProvider _identityProvider;
        private readonly IDeploymentQueue _deploymentQueue;
        private readonly IManagementClientProvider _clientProvider;
        private readonly IAzureResourceProvider _azureResourceProvider;
        private readonly ILogger _logger;

        public AssetRepoCoordinator(
            IConfigRepository<AssetRepository> configCoordinator,
            ITemplateProvider templateProvider,
            IIdentityProvider identityProvider,
            IDeploymentQueue deploymentQueue,
            IManagementClientProvider clientProvider,
            IAzureResourceProvider azureResourceProvider,
            ILogger<AssetRepoCoordinator> logger)
        {
            _configCoordinator = configCoordinator;
            _templateProvider = templateProvider;
            _identityProvider = identityProvider;
            _deploymentQueue = deploymentQueue;
            _clientProvider = clientProvider;
            _azureResourceProvider = azureResourceProvider;
            _logger = logger;
        }

        public async Task<List<string>> ListRepositories()
        {
            return await _configCoordinator.List();
        }

        public async Task<AssetRepository> GetRepository(string repoName)
        {
            return await _configCoordinator.Get(repoName);
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
            await _configCoordinator.Update(repository, repository.Name, originalName);
        }

        public async Task<bool> RemoveRepository(AssetRepository repository)
        {
            return await _configCoordinator.Remove(repository.Name);
        }

        //
        // Deployment operations
        //
        public async Task BeginRepositoryDeploymentAsync(AssetRepository repository)
        {
            using (var client = await _clientProvider.CreateResourceManagementClient(repository.SubscriptionId))
            {
                await client.ResourceGroups.CreateOrUpdateAsync(
                    repository.ResourceGroupName,
                    new ResourceGroup(
                        repository.Subnet.Location, // The subnet location pins us to a region
                        tags: AzureResourceProvider.GetEnvironmentTags(repository.EnvironmentName)));

                await _azureResourceProvider.AssignRoleToIdentityAsync(
                    repository.SubscriptionId,
                    repository.ResourceGroupResourceId,
                    AzureResourceProvider.ContributorRole,
                    _identityProvider.GetPortalManagedServiceIdentity());

                repository.DeploymentName = $"{repository.Name}-{Guid.NewGuid()}";

                await UpdateRepository(repository);

                await DeployRepository(repository);
            }
        }

        public async Task<ProvisioningState> UpdateRepositoryFromDeploymentAsync(AssetRepository repository)
        {
            var deployment = await GetDeploymentAsync(repository);
            if (deployment == null)
            {
                return ProvisioningState.Failed;
            }

            if (repository is NfsFileServer fileServer)
            {
                return await UpdateFileServerFromDeploymentAsync(fileServer);
            }
            else if (repository is AvereCluster avere)
            {
                return await UpdateAvereFromDeploymentAsync(avere);
            }

            throw new NotSupportedException("Unknown type of repository");
        }

        public async Task<ProvisioningState> UpdateFileServerFromDeploymentAsync(NfsFileServer fileServer)
        {
            var deployment = await GetDeploymentAsync(fileServer);
            if (deployment == null)
            {
                return ProvisioningState.Failed;
            }

            string privateIp = null;
            string publicIp = null;

            Enum.TryParse<ProvisioningState>(deployment.Properties.ProvisioningState, out var deploymentState);
            if (deploymentState == ProvisioningState.Succeeded)
            {
                (privateIp, publicIp) = await GetIpAddressesAsync(fileServer);
            }

            if (deploymentState == ProvisioningState.Succeeded || deploymentState == ProvisioningState.Failed)
            {
                fileServer.ProvisioningState = deploymentState;
                fileServer.PrivateIp = privateIp;
                fileServer.PublicIp = publicIp;
                await UpdateRepository(fileServer);
            }

            return deploymentState;
        }

        public async Task<ProvisioningState> UpdateAvereFromDeploymentAsync(AvereCluster avereCluster)
        {
            ProvisioningState provisioningState;

            var deployment = await GetDeploymentAsync(avereCluster);
            if (deployment == null)
            {
                provisioningState = ProvisioningState.Failed;
            }
            else
            {
                Enum.TryParse<ProvisioningState>(deployment.Properties.ProvisioningState, out provisioningState);
                if (provisioningState == ProvisioningState.Succeeded)
                {
                    avereCluster.ProvisioningState = provisioningState;
                    if (deployment.Properties.Outputs != null)
                    {
                        var outputs = deployment.Properties.Outputs as JObject;
                        avereCluster.SshConnectionDetails = (string)outputs["ssh_string"]?["value"];
                        avereCluster.ManagementIP = (string)outputs["mgmt_ip"]?["value"];
                        avereCluster.VServerIPRange = (string)outputs["vserver_ips"]?["value"];
                    }
                }
            }

            avereCluster.ProvisioningState = provisioningState;

            await UpdateRepository(avereCluster);

            return provisioningState;
        }

        public async Task BeginDeleteRepositoryAsync(AssetRepository repository)
        {
            repository.ProvisioningState = ProvisioningState.Deleting;
            await UpdateRepository(repository);
            await _deploymentQueue.Add(new ActiveDeployment
            {
                FileServerName = repository.Name,
                StartTime = DateTime.UtcNow,
                Action = "DeleteVM",
            });
        }

        public async Task DeleteRepositoryResourcesAsync(AssetRepository repository)
        {
            var fileServer = repository as NfsFileServer;
            if (fileServer == null)
            {
                return;
            }

            using (var resourceClient = await _clientProvider.CreateResourceManagementClient(repository.SubscriptionId))
            using (var computeClient = await _clientProvider.CreateComputeManagementClient(repository.SubscriptionId))
            using (var networkClient = await _clientProvider.CreateNetworkManagementClient(repository.SubscriptionId))
            {
                try
                {
                    var virtualMachine = await computeClient.VirtualMachines.GetAsync(fileServer.ResourceGroupName, fileServer.VmName);

                    var nicName = virtualMachine.NetworkProfile.NetworkInterfaces[0].Id.Split("/").Last(); ;
                    var avSetName = virtualMachine.AvailabilitySet.Id?.Split("/").Last();
                    var osDisk = virtualMachine.StorageProfile.OsDisk.ManagedDisk.Id.Split("/").Last();
                    var dataDisks = virtualMachine.StorageProfile.DataDisks.Select(dd => dd.ManagedDisk.Id.Split("/").Last()).ToList();

                    string pip = null;
                    string nsg = null;
                    try
                    {
                        var nic = await networkClient.NetworkInterfaces.GetAsync(fileServer.ResourceGroupName, nicName);
                        pip = nic.IpConfigurations[0].PublicIPAddress?.Id.Split("/").Last();
                        nsg = nic.NetworkSecurityGroup?.Id.Split("/").Last();
                    }
                    catch (CloudException ex) when (ResourceNotFound(ex))
                    {
                        // NIC doesn't exist
                    }

                    await IgnoreNotFound(async () =>
                    {
                        await computeClient.VirtualMachines.GetAsync(fileServer.ResourceGroupName, fileServer.VmName);
                        await computeClient.VirtualMachines.DeleteAsync(fileServer.ResourceGroupName, fileServer.VmName);
                    });

                    if (nicName != null)
                    {
                        await IgnoreNotFound(() => networkClient.NetworkInterfaces.DeleteAsync(fileServer.ResourceGroupName, nicName));
                    }

                    var tasks = new List<Task>();

                    if (nsg == "nsg")
                    {
                        tasks.Add(IgnoreNotFound(() => networkClient.NetworkSecurityGroups.DeleteAsync(fileServer.ResourceGroupName, nsg)));
                    }

                    if (pip != null)
                    {
                        tasks.Add(IgnoreNotFound(() => networkClient.PublicIPAddresses.DeleteAsync(fileServer.ResourceGroupName, pip)));
                    }

                    tasks.Add(IgnoreNotFound(() => computeClient.Disks.DeleteAsync(fileServer.ResourceGroupName, osDisk)));

                    tasks.AddRange(dataDisks.Select(
                        dd => IgnoreNotFound(() => computeClient.Disks.DeleteAsync(fileServer.ResourceGroupName, dd))));

                    await Task.WhenAll(tasks);

                    if (avSetName != null)
                    {
                        await IgnoreNotFound(() => computeClient.AvailabilitySets.DeleteAsync(fileServer.ResourceGroupName, avSetName));
                    }
                }
                catch (CloudException ex) when (ResourceNotFound(ex))
                {
                    // VM doesn't exist
                }

                try
                {
                    await resourceClient.ResourceGroups.GetAsync(fileServer.ResourceGroupName);

                    var resources = await resourceClient.Resources.ListByResourceGroupAsync(fileServer.ResourceGroupName);
                    if (resources.Any())
                    {
                        _logger.LogDebug($"Skipping resource group deletion as it contains the following resources: {string.Join(", ", resources.Select(r => r.Id))}");
                    }
                    else
                    {
                        await resourceClient.ResourceGroups.DeleteAsync(fileServer.ResourceGroupName);
                    }
                }
                catch (CloudException ex) when (ResourceNotFound(ex))
                {
                    // RG doesn't exist
                }

                await RemoveRepository(repository);
            }
        }

        private async Task<(string privateIp, string publicIp)> GetIpAddressesAsync(NfsFileServer fileServer)
        {
            using (var computeClient = await _clientProvider.CreateComputeManagementClient(fileServer.SubscriptionId))
            using (var networkClient = await _clientProvider.CreateNetworkManagementClient(fileServer.SubscriptionId))
            {
                var vm = await computeClient.VirtualMachines.GetAsync(fileServer.ResourceGroupName, fileServer.VmName);
                var networkIfaceName = vm.NetworkProfile.NetworkInterfaces.First().Id.Split("/").Last();
                var net = await networkClient.NetworkInterfaces.GetAsync(fileServer.ResourceGroupName, networkIfaceName);
                var firstConfiguration = net.IpConfigurations.First();

                var privateIp = firstConfiguration.PrivateIPAddress;
                var publicIpId = firstConfiguration.PublicIPAddress?.Id;
                var publicIp =
                    publicIpId != null
                        ? await networkClient.PublicIPAddresses.GetAsync(
                            fileServer.ResourceGroupName,
                            publicIpId.Split("/").Last())
                        : null;

                return (privateIp, publicIp?.IpAddress);
            }
        }

        private async Task<DeploymentExtended> GetDeploymentAsync(AssetRepository assetRepo)
        {
            using (var resourceClient = await _clientProvider.CreateResourceManagementClient(assetRepo.SubscriptionId))
            {
                try
                {
                    return await resourceClient.Deployments.GetAsync(
                        assetRepo.ResourceGroupName,
                        assetRepo.DeploymentName);
                }
                catch (CloudException e)
                {
                    if (ResourceNotFound(e))
                    {
                        return null;
                    }

                    throw;
                }
            }
        }

        private async Task DeployRepository(AssetRepository repository)
        {
            try
            {
                using (var client = await _clientProvider.CreateResourceManagementClient(repository.SubscriptionId))
                {
                    await client.ResourceGroups.CreateOrUpdateAsync(repository.ResourceGroupName,
                        new ResourceGroup { Location = repository.Subnet.Location });

                    var templateParams = repository.GetTemplateParameters();

                    var properties = new Deployment
                    {
                        Properties = new DeploymentProperties
                        {
                            Template = await _templateProvider.GetTemplate(repository.GetTemplateName()),
                            Parameters = _templateProvider.GetParameters(templateParams),
                            Mode = DeploymentMode.Incremental
                        }
                    };

                    // Start the ARM deployment
                    await client.Deployments.BeginCreateOrUpdateAsync(
                        repository.ResourceGroupName,
                        repository.DeploymentName,
                        properties);

                    // TODO re-enable below for background monitoring.
                    // Queue a request for the background host to monitor the deployment
                    // and update the state and IP address when it's done.
                    //await _deploymentQueue.Add(new ActiveDeployment
                    //{
                    //    FileServerName = repository.Name,
                    //    StartTime = DateTime.UtcNow,
                    //});

                    repository.ProvisioningState = ProvisioningState.Running;
                    repository.InProgress = false;

                    await UpdateRepository(repository);
                }
            }
            catch (CloudException ex)
            {
                _logger.LogError(ex, $"Failed to deploy NFS server: {ex.Message}.");
                throw;
            }
        }

        private static async Task IgnoreNotFound(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (CloudException e)
            {
                if (!ResourceNotFound(e))
                {
                    throw;
                }
            }
        }

        private static bool ResourceNotFound(CloudException ce)
        {
            return ce.Response.StatusCode == HttpStatusCode.NotFound;
        }
    }
}
