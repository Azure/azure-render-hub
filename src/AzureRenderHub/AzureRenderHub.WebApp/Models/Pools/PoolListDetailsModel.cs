// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.Azure.Management.Batch.Models;
using System.Collections.Generic;
using WebApp.Code.Extensions;
using WebApp.Config;

namespace WebApp.Models.Pools
{
    public class PoolListDetailsModel
    {
        public PoolListDetailsModel(Pool pool)
        {
            Name = pool.Name;
            CurrentDedicated = pool.CurrentDedicatedNodes ?? 0;
            TargetDedicated = pool.ScaleSettings.FixedScale.TargetDedicatedNodes ?? 0;
            TargetLowPriority = pool.ScaleSettings.FixedScale.TargetLowPriorityNodes ?? 0;
            CurrentDedicated = pool.CurrentDedicatedNodes ?? 0;
            CurrentLowPriority = pool.CurrentLowPriorityNodes ?? 0;
            VmSize = pool.VmSize;
            PoolAllocationState = pool.AllocationState;
            PoolProvisioningState = pool.ProvisioningState;
            AppLicenses = pool.ApplicationLicenses;
            AutoScaleEnabled = pool.GetAutoScalePolicy() != AutoScalePolicy.Disabled;
        }

        public string Name { get; }

        public int CurrentDedicated { get; }

        public int TargetDedicated { get; }

        public int CurrentLowPriority { get; }

        public int TargetLowPriority { get; }

        public string VmSize { get; }

        public IList<string> AppLicenses { get; }

        public AllocationState? PoolAllocationState { get; }

        public PoolProvisioningState? PoolProvisioningState { get; }

        public bool AutoScaleEnabled { get; }
    }
}