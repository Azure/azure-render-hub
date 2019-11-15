// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Batch;
using Microsoft.Azure.Management.Batch.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;
using Microsoft.WindowsAzure.Storage;
using WebApp.Code.Contract;
using WebApp.Code.Extensions;
using WebApp.Operations;
using WebApp.Providers.Resize;
using WebApp.Util;

namespace WebApp.BackgroundHosts.ScaleUpProcessor
{
    public class ScaleUpProcessorHost : BackgroundService
    {
        static readonly TimeSpan ResizeCheckDelay = TimeSpan.FromMinutes(2);

        readonly IScaleUpRequestStore _requestStore;
        readonly IManagementClientProvider _clientProvider;
        readonly AsyncAutoResetEvent _trigger;
        readonly IEnvironmentCoordinator _envs;
        readonly ILogger<ScaleUpProcessorHost> _logger;

        public ScaleUpProcessorHost(
            IScaleUpRequestStore requestStore,
            IEnvironmentCoordinator envs,
            ManagementClientMsiProvider batchClientProvider,
            AsyncAutoResetEvent trigger,
            ILogger<ScaleUpProcessorHost> logger)
        {
            _requestStore = requestStore;
            _clientProvider = batchClientProvider;
            _trigger = trigger;
            _logger = logger;
            _envs = envs;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // note that we don't want to call this on each loop iteration,
            // or we will create a backlog of waits that must be satisfied by the controller.
            // we only want one outstanding at any time
            var wait = _trigger.WaitAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var requests = await _requestStore.List(stoppingToken);
                    await Task.WhenAll(requests.Select(HandleRequest));
                }
                catch (Exception ex) when (!(ex is TaskCanceledException))
                {
                    _logger.LogError(ex, $"Unexpected error in {nameof(ScaleUpProcessorHost)} main loop");
                }

                var completed = await Task.WhenAny(Task.Delay(ResizeCheckDelay, stoppingToken), wait);
                if (completed == wait)
                {
                    // if our wait was completed we need to start a new one
                    wait = _trigger.WaitAsync();
                }
            }
        }

        private async Task HandleRequest(ScaleUpRequestEntity request)
        {
            try
            {

                switch (await PerformRequest(request))
                {
                    case RequestStatus.Completed:
                        try
                        {
                            await _requestStore.Delete(request);
                        }
                        catch (StorageException ex)
                            when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
                        {
                            // ETag mismatch - request was updated.
                            // We will pick it up again on the next loop.
                        }

                        break;

                    case RequestStatus.InProgress:
                        // Nothing to do.
                        break;
                }
            }
            catch (Exception ex) when (!(ex is TaskCanceledException))
            {
                _logger.LogError(ex, "Unexpected error when handling scale request '{0}'", request.ETag);
            }
        }

        enum RequestStatus
        {
            InProgress,
            Completed,
        }

        private async Task<RequestStatus> PerformRequest(ScaleUpRequestEntity request)
        {
            var env = await _envs.GetEnvironment(request.EnvironmentName);
            if (env == null)
            {
                _logger.LogInformation(
                    "Environment '{0}' has been deleted, discarding scale request '{1}'",
                    request.EnvironmentName,
                    request.ETag);

                return RequestStatus.Completed;
            }

            using (var batchClient = await _clientProvider.CreateBatchManagementClient(env.SubscriptionId))
            {

                try
                {
                    var pool = await batchClient.Pool.GetAsync(env.BatchAccount.ResourceGroupName, env.BatchAccount.Name, request.PoolName);
                    if (pool == null || pool.ProvisioningState == PoolProvisioningState.Deleting)
                    {
                        _logger.LogInformation(
                            "Pool '{0}' (in environment '{1}') has been deleted, discarding scale request '{2}'",
                            request.PoolName,
                            request.EnvironmentName,
                            request.ETag);

                        return RequestStatus.Completed;
                    }

                    if (pool.AllocationState == AllocationState.Resizing)
                    {
                        var op = pool.ResizeOperationStatus;
                        if (op != null && ((op.TargetDedicatedNodes ?? 0) + (op.TargetLowPriorityNodes ?? 0)) >= request.TargetNodes)
                        {
                            _logger.LogInformation(
                                "A resize operation on pool '{0}' (in environment '{1}') has made scale request '{2}' redundant, discarding it",
                                request.PoolName,
                                request.EnvironmentName,
                                request.ETag);

                            return RequestStatus.Completed;
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Pool '{0}' (in environment '{1}') is already being resized. Waiting to apply scale request '{2}'",
                                request.PoolName,
                                request.EnvironmentName,
                                request.ETag);

                            return RequestStatus.InProgress;
                        }
                    }

                    var targets = CalculateNodeTargets(request, pool);

                    var newPool =
                        new Pool(name: pool.Name)
                        {
                            ScaleSettings =
                                new ScaleSettings
                                {
                                    FixedScale =
                                        new FixedScaleSettings(
                                            targetLowPriorityNodes: targets.lowPriority,
                                            targetDedicatedNodes: targets.dedicated)
                                }
                        };

                    await batchClient.Pool.UpdateAsync(env.BatchAccount.ResourceGroupName, env.BatchAccount.Name, request.PoolName, newPool);

                    _logger.LogInformation(
                        "Successfully applied scale request '{0}' to pool '{1}' (in environment '{2}')",
                        request.ETag,
                        request.PoolName,
                        request.EnvironmentName);
                }
                catch (CloudException ce) when (ce.ResourceNotFound())
                {
                    // Pool is gone - complete the request to remove it.
                }

                return RequestStatus.Completed;
            }
        }

        private static (int lowPriority, int dedicated) CalculateNodeTargets(ScaleUpRequestEntity request, Pool pool)
            => NodeTargetsCalculator.Calculemus(
                request.TargetNodes,

                pool.ScaleSettings.FixedScale.TargetLowPriorityNodes ?? 0,
                pool.GetAutoScaleMinimumLowPriorityNodes(),
                pool.GetAutoScaleMaximumLowPriorityNodes(),

                pool.ScaleSettings.FixedScale.TargetDedicatedNodes ?? 0,
                pool.GetAutoScaleMinimumDedicatedNodes(),
                pool.GetAutoScaleMaximumDedicatedNodes());
    }


    // Standalone so we can test it without API objects
    public static class NodeTargetsCalculator
    {
        public static (int lowPriority, int dedicated) Calculemus(
            int targetNodes,
            int lowPrioCurrent, int lowPrioMin, int lowPrioMax,
            int dedicatedCurrent, int dedicatedMin, int dedicatedMax)
        {
            // respect the currently-allocated nodes:
            // we never want to scale down, and we want to count them towards
            // allocated nodes
            lowPrioMin = Math.Max(lowPrioCurrent, lowPrioMin);
            dedicatedMin = Math.Max(dedicatedCurrent, dedicatedMin);

            // allocate as many as possible to low-priority, respecting the dedicated min
            // any leftovers will be allocated to dedicated
            var targetLowPriority = Clamp(targetNodes - dedicatedMin, lowPrioMin, lowPrioMax);
            var targetDedicated = Clamp(targetNodes - targetLowPriority, dedicatedMin, dedicatedMax);

            return (targetLowPriority, targetDedicated);
        }

        private static int Clamp(int value, int minimum, int maximum)
            => Math.Min(Math.Max(value, minimum), maximum);
    }
}
