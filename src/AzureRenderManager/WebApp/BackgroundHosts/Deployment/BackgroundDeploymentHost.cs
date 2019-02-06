// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;
using WebApp.BackgroundHosts.LeaseMaintainer;
using WebApp.Code.Contract;
using WebApp.Config.Storage;
using WebApp.Operations;

namespace WebApp.BackgroundHosts.Deployment
{
    public class BackgroundDeploymentHost : BackgroundService
    {
        private readonly IAssetRepoCoordinator _assetRepoCoordinator;
        private readonly IManagementClientProvider _managementClientProvider;
        private readonly IDeploymentQueue _deploymentQueue;
        private readonly ILeaseMaintainer _leaseMaintainer;
        private readonly ILogger _logger;

        public BackgroundDeploymentHost(
            IAssetRepoCoordinator assetRepoCoordinator,
            IManagementClientProvider managementClientProvider,
            IDeploymentQueue deploymentQueue,
            ILeaseMaintainer leaseMaintainer,
            ILogger<BackgroundDeploymentHost> logger)
        {
            _assetRepoCoordinator = assetRepoCoordinator;
            _managementClientProvider = managementClientProvider;
            _deploymentQueue = deploymentQueue;
            _leaseMaintainer = leaseMaintainer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(15);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var activeDeployments = await _deploymentQueue.Get();
                    if (activeDeployments != null)
                    {
                        await Task.WhenAll(activeDeployments.Select(ExecuteMessage));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                await Task.Delay(interval, stoppingToken);
            }
        }

        private async Task ExecuteMessage(ActiveDeployment activeDeployment)
        {
            if (activeDeployment.Action == "DeleteVM")
            {
                await DeleteDeployment(activeDeployment);
            }
            else
            {
                await MonitorDeployment(activeDeployment);
            }
        }

        private async Task MonitorDeployment(ActiveDeployment activeDeployment)
        {
            _logger.LogDebug($"Waiting for file server {activeDeployment.FileServerName} deployment to complete");

            using (var cts = new CancellationTokenSource())
            {
                // runs in background
                var renewer = _leaseMaintainer.MaintainLease(activeDeployment, cts.Token);

                try
                {
                    var deploymentState = ProvisioningState.Running;

                    while (deploymentState == ProvisioningState.Running)
                    {
                        var fileServer = (NfsFileServer)await _assetRepoCoordinator.GetRepository(activeDeployment.FileServerName);
                        if (fileServer == null)
                        {
                            break;
                        }

                        var deployment = await GetDeploymentAsync(fileServer);
                        if (deployment == null)
                        {
                            break;
                        }

                        string privateIp = null;
                        string publicIp = null;

                        Enum.TryParse<ProvisioningState>(deployment.Properties.ProvisioningState, out deploymentState);
                        if (deploymentState != ProvisioningState.Running)
                        {
                            (privateIp, publicIp) = await GetIpAddressesAsync(fileServer);
                        }

                        fileServer = (NfsFileServer)await _assetRepoCoordinator.GetRepository(activeDeployment.FileServerName);
                        if (fileServer == null)
                        {
                            break;
                        }

                        if (fileServer.ProvisioningState != deploymentState || fileServer.PrivateIp != privateIp)
                        {
                            fileServer.ProvisioningState = deploymentState;
                            fileServer.PrivateIp = privateIp;
                            fileServer.PublicIp = publicIp;
                            await _assetRepoCoordinator.UpdateRepository(fileServer);
                        }

                        if (deploymentState == ProvisioningState.Running)
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(15));
                        }
                    }
                }
                finally
                {
                    await _deploymentQueue.Delete(activeDeployment.MessageId, activeDeployment.PopReceipt);

                    cts.Cancel();
                    try
                    {
                        await renewer;
                    }
                    catch (OperationCanceledException)
                    {
                        // expected
                    }
                }
            }
        }

        private async Task DeleteDeployment(ActiveDeployment activeDeployment)
        {
            _logger.LogDebug($"Deleting file server {activeDeployment.FileServerName}");

            var fileServer = (NfsFileServer) await _assetRepoCoordinator.GetRepository(activeDeployment.FileServerName);
            if (fileServer == null)
            {
                _logger.LogDebug($"File server {activeDeployment.FileServerName} config has already been deleted.");
                await _deploymentQueue.Delete(activeDeployment.MessageId, activeDeployment.PopReceipt);
                return;
            }

            using (var resourceClient = await _managementClientProvider.CreateResourceManagementClient(Guid.Parse(fileServer.SubscriptionId)))
            using (var computeClient = await _managementClientProvider.CreateComputeManagementClient(Guid.Parse(fileServer.SubscriptionId)))
            using (var networkClient = await _managementClientProvider.CreateNetworkManagementClient(Guid.Parse(fileServer.SubscriptionId)))
            using (var cts = new CancellationTokenSource())
            {
                // runs in background
                var renewer = _leaseMaintainer.MaintainLease(activeDeployment, cts.Token);

                try
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
                        catch (CloudException ex) when (ex.Body.Code == "ResourceNotFound")
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
                    catch (CloudException ex) when(ex.Body.Code == "ResourceNotFound")
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
                    catch (CloudException ex) when (ex.Body.Code == "ResourceNotFound")
                    {
                        // RG doesn't exist
                    }

                    await _assetRepoCoordinator.RemoveRepository(fileServer);

                    await _deploymentQueue.Delete(activeDeployment.MessageId, activeDeployment.PopReceipt);
                }
                catch (CloudException e)
                {
                    _logger.LogError(e, $"Error deleting file server {activeDeployment.FileServerName}");
                }
                finally
                {
                    cts.Cancel();

                    try
                    {
                        await renewer;
                    }
                    catch (OperationCanceledException)
                    {
                        // expected
                    }
                }
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
                if (e.Body.Code != "ResourceNotFound")
                {
                    throw;
                }
            }
        }

        private async Task<DeploymentExtended> GetDeploymentAsync(AssetRepository assetRepo)
        {
            using (var resourceClient = await _managementClientProvider.CreateResourceManagementClient(Guid.Parse(assetRepo.SubscriptionId)))
            {
                try
                {
                    return await resourceClient.Deployments.GetAsync(
                        assetRepo.ResourceGroupName,
                        assetRepo.DeploymentName);
                }
                catch (CloudException e)
                {
                    if (e.Body.Code == "ResourceNotFound")
                    {
                        return null;
                    }

                    throw;
                }
            }
        }

        private async Task<(string privateIp, string publicIp)> GetIpAddressesAsync(NfsFileServer fileServer)
        {
            using (var computeClient = await _managementClientProvider.CreateComputeManagementClient(Guid.Parse(fileServer.SubscriptionId)))
            using (var networkClient = await _managementClientProvider.CreateNetworkManagementClient(Guid.Parse(fileServer.SubscriptionId)))
            {
                var vm = await computeClient.VirtualMachines.GetAsync(fileServer.ResourceGroupName, fileServer.VmName);
                var networkIfaceName = vm.NetworkProfile.NetworkInterfaces.First().Id.Split("/").Last();
                var net = await networkClient.NetworkInterfaces.GetAsync(fileServer.ResourceGroupName, networkIfaceName);

                var privateIp = net.IpConfigurations.First().PrivateIPAddress;
                string publicIp = null;

                if (net.IpConfigurations.First().PublicIPAddress != null &&
                    net.IpConfigurations.First().PublicIPAddress.Id != null)
                {
                    var pip = await networkClient.PublicIPAddresses.GetAsync(
                        fileServer.ResourceGroupName,
                        net.IpConfigurations.First().PublicIPAddress.Id.Split("/").Last());

                    publicIp = pip.IpAddress;
                }

                return (privateIp, publicIp);
            }
        }
    }
}
