// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApp.Config;

namespace WebApp.BackgroundHosts.AutoScale
{
    public interface IActiveNodeProvider
    {
        Task<List<ActiveComputeNode>> GetActiveComputeNodes(RenderingEnvironment config);
    }

    public class ActiveComputeNode
    {
        public DateTime LastActive { get; set; }

        public string PoolName { get; set; }

        public string ComputeNodeName { get; set; }

        public bool TrackedProcess { get; set; }

        public long CpuPercent { get; set; }

        public long GpuPercent { get; set; }
    }
}
