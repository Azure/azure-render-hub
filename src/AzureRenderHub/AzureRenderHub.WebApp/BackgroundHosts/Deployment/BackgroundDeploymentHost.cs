// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureRenderHub.WebApp.Arm.Deploying;
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
            if (activeDeployment.DequeueCount > 10)
            {
                _logger.LogWarning(
                    $"Deleting background task for {activeDeployment.StorageName} as it has exceeded the " +
                    $"maximum dequeue count.");
                await _deploymentQueue.Delete(activeDeployment.MessageId, activeDeployment.PopReceipt);
            }
            else if (activeDeployment.Action == "DeleteVM")
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
            _logger.LogDebug($"Waiting for storage {activeDeployment.StorageName} deployment to complete");

            using (var cts = new CancellationTokenSource())
            {
                // runs in background
                var renewer = _leaseMaintainer.MaintainLease(activeDeployment, cts.Token);

                try
                {
                    var deploymentState = ProvisioningState.Running;

                    while (deploymentState == ProvisioningState.Running)
                    {
                        var fileServer = (NfsFileServer)await _assetRepoCoordinator.GetRepository(activeDeployment.StorageName);
                        if (fileServer == null)
                        {
                            break;
                        }

                        await _assetRepoCoordinator.UpdateRepositoryFromDeploymentAsync(fileServer);

                        deploymentState = fileServer.Deployment.ProvisioningState;

                        _logger.LogDebug($"[MonitorDeployment={activeDeployment.StorageName}] " +
                            $"Deployment returned state {deploymentState}");

                        if (deploymentState == ProvisioningState.Running)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(15), cts.Token);
                        }
                    }
                }
                finally
                {
                    _logger.LogDebug($"[MonitorDeployment={activeDeployment.StorageName}] " +
                        $"Deleting queue message {activeDeployment.MessageId} " +
                        $"with receipt {activeDeployment.PopReceipt}");

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

        private async Task DeleteDeployment(ActiveDeployment storageDeployment)
        {
            _logger.LogDebug($"[DeleteStorage={storageDeployment.StorageName}] Deleting storage");

            using (var cts = new CancellationTokenSource())
            {
                // runs in background
                var renewer = _leaseMaintainer.MaintainLease(storageDeployment, cts.Token);

                try
                {
                    var repository = await _assetRepoCoordinator.GetRepository(storageDeployment.StorageName);
                    if (repository != null)
                    {
                        await _assetRepoCoordinator.DeleteRepositoryResourcesAsync(repository, storageDeployment.DeleteResourceGroup);
                    }

                    _logger.LogDebug($"[DeleteStorage={storageDeployment.StorageName}] Deleting queue message {storageDeployment.MessageId} with receipt {storageDeployment.PopReceipt}");

                    await _deploymentQueue.Delete(storageDeployment.MessageId, storageDeployment.PopReceipt);
                }
                catch (CloudException e)
                {
                    _logger.LogError(e, $"[DeleteStorage={storageDeployment.StorageName}]");
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
