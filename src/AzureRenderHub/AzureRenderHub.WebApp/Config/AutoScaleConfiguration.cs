// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Config
{
    public class AutoScaleConfiguration
    {
        private const int DefaultIdlePercent = 5;

        public AutoScaleConfiguration()
        {
            Policy = AutoScalePolicy.Disabled;
            MaxIdleCpuPercent = DefaultIdlePercent;
        }

        public AutoScalePolicy Policy { get; set; }

        // Max avg CPU percent to consider a node idle
        public int MaxIdleCpuPercent { get; set; }

        public int MaxIdleGpuPercent { get; set; }

        // List of processes that indicate a node is busy
        public List<string> SpecificProcesses { get; set; }

        // When enabled an endpoint will be exposed for this environment to scale the pools
        public bool ScaleEndpointEnabled { get; set; }

        // The secure authentication key required for access
        public string PrimaryApiKey { get; set; }

        public string SecondaryApiKey { get; set; }
    }

    public enum AutoScalePolicy
    {
        [Description("Disabled")]
        Disabled,

        [Description("Resources (CPU and GPU)")]
        Resources,

        [Description("Specific Processes")]
        SpecificProcesses,

        [Description("Resources + Specific Processes")]
        ResourcesAndSpecificProcesses,
    }
}
