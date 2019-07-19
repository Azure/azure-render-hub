// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly IDeploymentQueue _deploymentQueue;
        private readonly ILeaseMaintainer _leaseMaintainer;
        private readonly ILogger _logger;

        public BackgroundDeploymentHost(
            IAssetRepoCoordinator assetRepoCoordinator,
            IDeploymentQueue deploymentQueue,
            ILeaseMaintainer leaseMaintainer,
            ILogger<BackgroundDeploymentHost> logger)
        {
            _assetRepoCoordinator = assetRepoCoordinator;
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

                        deploymentState = await _assetRepoCoordinator.UpdateRepositoryFromDeploymentAsync(fileServer);

                        if (deploymentState == ProvisioningState.Running)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(15), cts.Token);
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

            using (var cts = new CancellationTokenSource())
            {
                // runs in background
                var renewer = _leaseMaintainer.MaintainLease(activeDeployment, cts.Token);

                try
                {
                    var repository = await _assetRepoCoordinator.GetRepository(activeDeployment.FileServerName);
                    if (repository != null)
                    {
                        await _assetRepoCoordinator.DeleteRepositoryResourcesAsync(repository);
                    }

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
    }
}
