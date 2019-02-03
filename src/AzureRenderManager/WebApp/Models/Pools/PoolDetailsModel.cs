// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Azure.Management.Batch.Models;
using WebApp.AppInsights.PoolUsage;
using WebApp.Code;
using WebApp.Code.Extensions;
using WebApp.Config;

namespace WebApp.Models.Pools
{
    public class PoolDetailsModel : PoolBaseModel
    {
        public PoolDetailsModel()
        { }

        public PoolDetailsModel(RenderingEnvironment environment, Pool pool, IList<PoolUsageMetric> poolUsageMetrics = null)
        {
            Name = pool.Name;
            DisplayName = pool.DisplayName;
            EnvironmentName = environment.Name;
            RenderManagerType = environment.RenderManager;

            // TODO: handle autoscale
            DedicatedNodes = pool.ScaleSettings.FixedScale.TargetDedicatedNodes ?? 0;
            LowPriorityNodes = pool.ScaleSettings.FixedScale.TargetLowPriorityNodes ?? 0;

            CurrentDedicatedNodes = pool.CurrentDedicatedNodes ?? 0;
            CurrentLowPriorityNodes = pool.CurrentLowPriorityNodes ?? 0;

            VmSize = pool.VmSize;

            // TODO: handle non-VM config
            ImageReference = pool.DeploymentConfiguration.VirtualMachineConfiguration.ImageReference.Sku;
            BatchAgentSku = pool.DeploymentConfiguration.VirtualMachineConfiguration.NodeAgentSkuId;
            AllocationState = pool.AllocationState;
            ProvisioningState = pool.ProvisioningState;

            if (pool.Metadata != null)
            {
                AutoScaleDownIdleTimeout = pool.GetAutoScaleTimeoutInMinutes();
                AutoScalePolicy = pool.GetAutoScalePolicy();
                AutoScalePolicyItems.AddRange(
                    Enum.GetValues(typeof(AutoScalePolicy)).Cast<AutoScalePolicy>()
                        .Select(p => new SelectListItem(p.GetDescription(), p.ToString(), p == AutoScalePolicy)));
                MinimumDedicatedNodes = pool.GetAutoScaleMinimumDedicatedNodes();
                MinimumLowPriorityNodes = pool.GetAutoScaleMinimumLowPriorityNodes();
                MaximumDedicatedNodes = pool.GetAutoScaleMaximumDedicatedNodes();
                MaximumLowPriorityNodes = pool.GetAutoScaleMaximumLowPriorityNodes();

                // TODO: This seems rather dodgy. What if there are more than one package?
                SelectedPackageId = pool.Metadata.FirstOrDefault(mi => mi.Name == MetadataKeys.Package)?.Value;
                SelectedGpuPackageId = pool.Metadata.FirstOrDefault(mi => mi.Name == MetadataKeys.GpuPackage)?.Value;
                SelectedGeneralPackageIds = pool.Metadata.FirstOrDefault(mi => mi.Name == MetadataKeys.GeneralPackages)?.Value;

                bool.TryParse(pool.Metadata.FirstOrDefault(mi => mi.Name == MetadataKeys.UseDeadlineGroups)?.Value, out var useGroups);
                UseGroups = useGroups;
            }

            ApplicationLicenses = pool.ApplicationLicenses != null
                ? string.Join(", ", pool.ApplicationLicenses)
                : "n/a";

            PoolUsageMetrics = poolUsageMetrics;
        }

        public RenderManagerType RenderManagerType { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string SelectedPackageId { get; set; }

        public string SelectedGpuPackageId { get; set; }

        public string SelectedGeneralPackageIds { get; set; }

        public string ImageReference { get; set; }

        public string BatchAgentSku { get; set; }

        public string VmSize { get; set; }

        public string ApplicationLicenses { get; }

        public AllocationState? AllocationState { get; }

        public PoolProvisioningState? ProvisioningState { get; }

        public int TargetDedicatedNodes { get; set; }

        public int TargetLowPriorityNodes { get; set; }

        // Updateable
        [Required]
        [Range(0, 1_000)]
        public int DedicatedNodes { get; set; }

        [Required]
        [Range(0, 1_000)]
        public int LowPriorityNodes { get; set; }

        public int CurrentDedicatedNodes { get; set; }

        public int CurrentLowPriorityNodes { get; set; }

        public List<SelectListItem> AutoScalePolicyItems { get; } = new List<SelectListItem>();

        public bool UseGroups { get; set; }

        public IList<PoolUsageMetric> PoolUsageMetrics { get; set; }
    }
}
