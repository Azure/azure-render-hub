// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Config;

namespace WebApp.AppInsights.PoolUsage
{
    public interface IPoolUsageProvider
    {
        Task<IList<PoolUsageResult>> GetEnvironmentUsage(RenderingEnvironment environment);

        Task<PoolUsageResult> GetUsageForPool(RenderingEnvironment environment, string poolName, string vmSize);
    }
}
