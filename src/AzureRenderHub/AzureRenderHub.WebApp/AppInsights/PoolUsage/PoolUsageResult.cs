// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.AppInsights.PoolUsage
{
    public class PoolUsageResult
    {
        public string PoolName { get; set; }

        public IList<PoolUsageMetric> Values { get; set; } = new List<PoolUsageMetric>();
    }
}
