// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.AppInsights.PoolUsage
{
    public class PoolUsageMetric
    {
        public DateTime Timestamp { get; set; }

        public string PoolName { get; set; }

        public int TotalNodes { get; set; }

        public int TotalCores { get; set; }

        public int DedicatedNodes { get; set; }

        public int DedicatedCores { get; set; }

        public int LowPriorityNodes { get; set; }

        public int LowPriorityCores { get; set; }
    }
}
