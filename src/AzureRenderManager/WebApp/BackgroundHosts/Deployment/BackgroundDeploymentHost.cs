// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Rest;
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

        public BackgroundDeploymentHost(
            IAssetRepoCoordinator assetRepoCoordinator,
            IManagementClientProvider managementClientProvider,
            IDeploymentQueue deploymentQueue,
            ILeaseMaintainer leaseMaintainer)
        {
            _assetRepoCoordinator = assetRepoCoordinator;
            _managementClientProvider = managementClientProvider;
            _deploymentQueue = deploymentQueue;
            _leaseMaintainer = leaseMaintainer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(15);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var activeDeployment = await _deploymentQueue.Get();
                    if (activeDeployment != null)
                    {
                        if (activeDeployment.Action != null && activeDeployment.Action == "DeleteVM")
                        {
                            await DeleteDeployment(activeDeployment);
                        }
                        else
                        {
                            await MonitorDeployment(activeDeployment);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                await Task.Delay(interval, stoppingToken);
            }
        }

        private async Task MonitorDeployment(ActiveDeployment activeDeployment)
        {
            var state = "Succeeded";

            var fileServer = (NfsFileServer) await _assetRepoCoordinator.GetRepository(activeDeployment.FileServerName);
            if (fileServer != null)
            {
                string privateIp = null;
                string publicIp = null;

                var deployment = await GetDeploymentAsync(fileServer);
                if (deployment != null)
                {
                    state = deployment.Properties.ProvisioningState;
                    if (state != "Running")
                    {
                        (privateIp, publicIp) = await GetIpAddressesAsync(fileServer);
                    }
                }

                fileServer = (NfsFileServer) await _assetRepoCoordinator.GetRepository(activeDeployment.FileServerName);
                if (fileServer != null && (fileServer.ProvisioningState != state || privateIp != null))
                {
                    fileServer.ProvisioningState = state;
                    fileServer.PrivateIp = privateIp;
                    fileServer.PublicIp = publicIp;
                    await _assetRepoCoordinator.UpdateRepository(fileServer);
                }
            }

            if (fileServer == null || state != "Running")
            {
                await _deploymentQueue.Delete(activeDeployment.MessageId, activeDeployment.PopReceipt);
            }
        }

        private async Task DeleteDeployment(ActiveDeployment activeDeployment)
        {
            var fileServer = (NfsFileServer) await _assetRepoCoordinator.GetRepository(activeDeployment.FileServerName);
            if (fileServer == null)
            {
                return;
            }

            using (var computeClient = await _managementClientProvider.CreateComputeManagementClient(Guid.Parse(fileServer.SubscriptionId)))
            using (var networkClient = await _managementClientProvider.CreateNetworkManagementClient(Guid.Parse(fileServer.SubscriptionId)))
            using (var cts = new CancellationTokenSource())
            {
                // runs in background
                var renewer = _leaseMaintainer.MaintainLease(activeDeployment, cts.Token);

                try
                {
                    await IgnoreNotFound(async () =>
                    {
                        var vm = await computeClient.VirtualMachines.GetAsync(fileServer.ResourceGroupName, fileServer.VmName);
                        await computeClient.VirtualMachines.DeleteAsync(fileServer.ResourceGroupName, fileServer.VmName);
                    });

                    if (activeDeployment.NicName != null)
                    {
                        await IgnoreNotFound(() => networkClient.NetworkInterfaces.DeleteAsync(fileServer.ResourceGroupName, activeDeployment.NicName));
                    }

                    if (activeDeployment.NsgName != null)
                    {
                        await IgnoreNotFound(() => networkClient.NetworkSecurityGroups.BeginDeleteAsync(fileServer.ResourceGroupName, activeDeployment.NsgName));
                    }

                    if (activeDeployment.PipName != null)
                    {
                        await IgnoreNotFound(() => networkClient.PublicIPAddresses.BeginDeleteAsync(fileServer.ResourceGroupName, activeDeployment.PipName));
                    }

                    await IgnoreNotFound(() => computeClient.Disks.BeginDeleteAsync(fileServer.ResourceGroupName, activeDeployment.OsDiskName));

                    await Task.WhenAll(
                        activeDeployment.DataDiskNames.Select(
                            dd => IgnoreNotFound(() => computeClient.Disks.BeginDeleteAsync(fileServer.ResourceGroupName, dd))));

                    if (activeDeployment.AvSetName != null)
                    {
                        await IgnoreNotFound(() => computeClient.AvailabilitySets.DeleteAsync(fileServer.ResourceGroupName, activeDeployment.AvSetName));
                    }

                    await _assetRepoCoordinator.RemoveRepository(fileServer);
                }
                catch (CloudException e)
                {
                    if (e.Body != null && e.Body.Code != null && e.Body.Code == "ExpiredAuthenticationToken")
                    {
                        await _deploymentQueue.Delete(activeDeployment.MessageId, activeDeployment.PopReceipt);
                    }
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
                catch (CloudException)
                {
                    return null;
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
