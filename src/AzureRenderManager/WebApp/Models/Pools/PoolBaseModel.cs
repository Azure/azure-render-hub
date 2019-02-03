// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebApp.Config;

namespace WebApp.Models.Pools
{
    public class PoolBaseModel
    {
        [Required]
        public string EnvironmentName { get; set; }

        [Required]
        [EnumDataType(typeof(AutoScalePolicy))]
        public AutoScalePolicy AutoScalePolicy { get; set; }

        [Range(0, 120)]
        public int AutoScaleDownIdleTimeout { get; set; }

        [Required]
        [Range(0, 10_000)]
        public int MinimumDedicatedNodes { get; set; }

        [Required]
        [Range(0, 10_000)]
        public int MinimumLowPriorityNodes { get; set; }

        [Required]
        [Range(0, 10_000)]
        public int MaximumDedicatedNodes { get; set; }

        [Required]
        [Range(0, 10_000)]
        public int MaximumLowPriorityNodes { get; set; }
    }
}
