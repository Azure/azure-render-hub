// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebApp.Code;
using WebApp.Code.Contract;
using WebApp.Code.Extensions;
using WebApp.Config;
using WebApp.Operations;

namespace WebApp.BackgroundHosts.AutoScale
{
    public class AutoScaleHost : BackgroundService
    {
        private readonly IEnvironmentCoordinator _environments;
        private readonly BatchClientMsiProvider _batchClientAccessor;
        private readonly IActiveNodeProvider _activeNodeProvider;
        readonly ILogger<AutoScaleHost> _logger;

        public AutoScaleHost(IEnvironmentCoordinator environments,
            BatchClientMsiProvider batchClientAccessor,
            IActiveNodeProvider activeNodeProvider,
            ILogger<AutoScaleHost> logger)
        {
            _environments = environments;
            _batchClientAccessor = batchClientAccessor;
            _activeNodeProvider = activeNodeProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(60);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var environments = await _environments.ListEnvironments();
                    await Task.WhenAll(environments.Select(async e => await AutoScaleEnvironment(await _environments.GetEnvironment(e))));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unexpected error in {nameof(AutoScaleHost)} main loop");
                }

                await Task.Delay(interval, stoppingToken);
            }
        }

        private async Task<List<CloudPool>> GetAutoScalePools(BatchClient client)
        {
            var pools = await client.PoolOperations.ListPools().ToListAsync();
            return pools.Where(p => p.Metadata != null && p.Metadata.Any(m => p.GetAutoScalePolicy() != AutoScalePolicy.Disabled)).ToList();
        }

        private async Task AutoScaleEnvironment(RenderingEnvironment environment)
        {
            BatchClient client = null;

            try
            {
                if (environment == null)
                {
                    _logger.LogTrace($"Skipping null environment");
                }

                if (environment.InProgress ||
                    environment.BatchAccount == null)
                {
                    _logger.LogTrace($"Skipping environment {environment.Name}, InProgress={environment.InProgress}, HasBatchAccount={environment.BatchAccount != null}");
                    return;
                }

                client = _batchClientAccessor.CreateBatchClient(environment);

                // Pools with auto scale enabled
                List<CloudPool> pools = await GetAutoScalePools(client);

                if (!pools.Any())
                {
                    // Check for pools first so we don't query app insights
                    // unnecessarily
                    return;
                }

                // All active nodes for the environment
                var activeNodes = await _activeNodeProvider.GetActiveComputeNodes(environment);

                foreach (var pool in pools)
                {
                    // Verify pool can be resized
                    if (pool.State.Value != PoolState.Active ||
                        pool.AllocationState.Value != AllocationState.Steady)
                    {
                        _logger.LogInformation($"Autoscale for Env {environment.Name} and Pool {pool.Id}: Skipping pool State {pool.State.Value}, AllocationState {pool.AllocationState.Value}");
                        continue;
                    }

                    var policy = pool.GetAutoScalePolicy();
                    var timeout = pool.GetAutoScaleTimeoutInMinutes();

                    var minDedicated = pool.GetAutoScaleMinimumDedicatedNodes();
                    var minLowPriority = pool.GetAutoScaleMinimumLowPriorityNodes();

                    // Ensure it cannot be zero
                    var maxIdleCpuPercent = environment.AutoScaleConfiguration.MaxIdleCpuPercent > 0
                        ? environment.AutoScaleConfiguration.MaxIdleCpuPercent
                        : 1;

                    // Ensure it cannot be zero
                    var maxIdleGpuPercent = environment.AutoScaleConfiguration.MaxIdleGpuPercent > 0
                        ? environment.AutoScaleConfiguration.MaxIdleGpuPercent
                        : 1;

                    _logger.LogInformation($"Autoscale for Env {environment.Name} and Pool {pool.Id}: " +
                                      $"policy {policy}, " +
                                      $"timeout {timeout}, " +
                                      $"currentDedicated {pool.CurrentDedicatedComputeNodes.Value}, " +
                                      $"currentLowPriority {pool.CurrentLowPriorityComputeNodes.Value}, " +
                                      $"minimumDedicated {minDedicated}, " +
                                      $"minimumLowPriority {minLowPriority}, " + 
                                      $"maxIdleCpuPercent {maxIdleCpuPercent}, " +
                                      $"maxIdleGpuPercent {maxIdleGpuPercent}");

                    // Last acceptable active timestamp
                    var idleTimeCutoff = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(timeout));

                    // This pools nodes with CPU or whitelisted process events
                    var poolNodeCpuAndProcessEvents =
                        activeNodes.Where(an => an.PoolName == pool.Id && an.LastActive > idleTimeCutoff).ToList();

                    _logger.LogInformation($"Autoscale for Env {environment.Name} and Pool {pool.Id}: " +
                                      $"Nodes with process events " +
                                      $"{poolNodeCpuAndProcessEvents.Where(e => e.TrackedProcess).Select(e => e.ComputeNodeName).Distinct().Count()}");

                    _logger.LogInformation($"Autoscale for Env {environment.Name} and Pool {pool.Id}: " +
                                      $"Nodes with CPU events " +
                                      $"{poolNodeCpuAndProcessEvents.Where(e => !e.TrackedProcess).Select(e => e.ComputeNodeName).Distinct().Count()}");

                    // All nodes in the pool eligible for eviction.
                    // We ensure there's at least some CPU events lately to ensure
                    // app insights is running and emitting events.
                    var eligibleNodes = FilterNodesEligibleForEviction(pool.ListComputeNodes().ToList(), timeout)
                        .Where(n => poolNodeCpuAndProcessEvents.Any(pn => n.Id == pn.ComputeNodeName))
                        .ToList();

                    _logger.LogInformation($"Autoscale for Env {environment.Name} and Pool {pool.Id}: " +
                                      $"Eligible nodes " +
                                      $"{eligibleNodes.Count}");

                    var activeNodeByCpuNames = new HashSet<string>();
                    var activeNodeByGpuNames = new HashSet<string>();
                    var activeNodesByProcess = new HashSet<string>();

                    if (policy == AutoScalePolicy.Resources || policy == AutoScalePolicy.ResourcesAndSpecificProcesses)
                    {
                        activeNodeByCpuNames = poolNodeCpuAndProcessEvents.Where(an =>
                                !an.TrackedProcess && // Grab nodes with CPU usage (not whitelisted)
                                an.CpuPercent >=
                                maxIdleCpuPercent) // Over the idle CPU % limit
                            .Select(an => an.ComputeNodeName)
                            .ToHashSet();

                        activeNodeByGpuNames = poolNodeCpuAndProcessEvents.Where(an =>
                                !an.TrackedProcess && // Grab nodes with GPU usage (not whitelisted)
                                an.GpuPercent >=
                                maxIdleGpuPercent) // Over the idle GPU % limit
                            .Select(an => an.ComputeNodeName)
                            .ToHashSet();
                    }

                    if (policy == AutoScalePolicy.SpecificProcesses ||
                        policy == AutoScalePolicy.ResourcesAndSpecificProcesses)
                    {
                        activeNodesByProcess = poolNodeCpuAndProcessEvents.Where(an => an.TrackedProcess)
                            .Select(an => an.ComputeNodeName)
                            .ToHashSet();
                    }

                    _logger.LogInformation($"Autoscale for Env {environment.Name} and Pool {pool.Id}: " +
                            $"Active Nodes by Process ({activeNodesByProcess.Count}): " +
                            $"{string.Join(",", activeNodesByProcess.Take(5))}...");

                    _logger.LogInformation($"Autoscale for Env {environment.Name} and Pool {pool.Id}: " +
                            $"Active Nodes by CPU ({activeNodeByCpuNames.Count}): " +
                            $"{string.Join(",", activeNodeByCpuNames.Take(5))}...");

                    _logger.LogInformation($"Autoscale for Env {environment.Name} and Pool {pool.Id}: " +
                            $"Active Nodes by GPU ({activeNodeByGpuNames.Count}): " +
                            $"{string.Join(",", activeNodeByGpuNames.Take(5))}...");

                    var idleNodesToShutdown = eligibleNodes.Where(
                        cn => !activeNodesByProcess.Contains(cn.Id) &&
                              !activeNodeByCpuNames.Contains(cn.Id) &&
                              !activeNodeByGpuNames.Contains(cn.Id)).ToList();

                    _logger.LogInformation($"Autoscale for Env {environment.Name} and Pool {pool.Id}: " +
                            $"All Selected Idle nodes " +
                            $"{idleNodesToShutdown.Count}");

                    // Remove the idle nodes
                    if (idleNodesToShutdown.Any())
                    {
                        var dedicatedNodesToShutdown = idleNodesToShutdown.Where(n => n.IsDedicated.HasValue && n.IsDedicated.Value);
                        var lowPriorityNodesToShutdown = idleNodesToShutdown.Where(n => n.IsDedicated.HasValue && !n.IsDedicated.Value);

                        var maxDedicatedToRemove = pool.CurrentDedicatedComputeNodes.Value < minDedicated
                            ? 0
                            : pool.CurrentDedicatedComputeNodes.Value - minDedicated;

                        var maxLowPriorityToRemove = pool.CurrentLowPriorityComputeNodes.Value < minLowPriority
                            ? 0
                            : pool.CurrentLowPriorityComputeNodes.Value - minLowPriority;

                        _logger.LogInformation($"Autoscale for Env {environment.Name} and Pool {pool.Id}: " +
                                          $"Max nodes to remove: " +
                                          $"maxDedicatedToRemove {maxDedicatedToRemove}, " +
                                          $"maxLowPriorityToRemove {maxLowPriorityToRemove}");

                        var safeNodesToRemove = new List<ComputeNode>();
                        safeNodesToRemove.AddRange(dedicatedNodesToShutdown.Take(maxDedicatedToRemove));
                        safeNodesToRemove.AddRange(lowPriorityNodesToShutdown.Take(maxLowPriorityToRemove));

                        _logger.LogInformation($"Autoscale for Env {environment.Name} and Pool {pool.Id}: " +
                                          $"Removing nodes: " +
                                          $"{safeNodesToRemove.Count}");

                        if (safeNodesToRemove.Any())
                        {
                            try
                            {
                                await pool.RemoveFromPoolAsync(safeNodesToRemove.Take(100));
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, $"Error removing nodes from pool {pool.Id} in environment {environment.Name}");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error executing auto scale for environment {environment?.Name}");
            }
            finally
            {
                if (client != null)
                {
                    client.Dispose();
                }
            }
        }

        private List<ComputeNode> FilterNodesEligibleForEviction(List<ComputeNode> computeNodes, int timeout)
        {
            var validStates = new[]
            {
                ComputeNodeState.Idle,
                ComputeNodeState.Offline,
                ComputeNodeState.Preempted,
                ComputeNodeState.Running,
                ComputeNodeState.StartTaskFailed,
                ComputeNodeState.Unusable,
                ComputeNodeState.Unknown,
            };

            var idleTimeCutoff = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(timeout));

            var eligibleComputeNodes = new List<ComputeNode>();
            foreach (var computeNode in computeNodes)
            {
                // Make sure the nodes have a valid state, i.e. they've at least started
                if (validStates.Contains(computeNode.State.Value) &&
                    computeNode.LastBootTime.HasValue &&
                    computeNode.StateTransitionTime.HasValue)
                {
                    // Ensure the node has been in the current state for 'x' minutes to avoid
                    // evicting a node the has been booting/prepping for 'x' minutes but has exceeded the idle timeout the idle timeout
                    var lastChange = computeNode.LastBootTime.Value > computeNode.StateTransitionTime.Value
                        ? computeNode.LastBootTime.Value
                        : computeNode.StateTransitionTime.Value;

                    if (lastChange < idleTimeCutoff)
                    {
                        eligibleComputeNodes.Add(computeNode);
                    }
                }
            }
            return eligibleComputeNodes;
        }
    }
}
