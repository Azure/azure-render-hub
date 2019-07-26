// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebApp.BackgroundHosts.AutoScale;
using WebApp.Config;

namespace WebApp.AppInsights.ActiveNodes
{
    public class ActiveNodeProvider : IActiveNodeProvider
    {
        private const string ActiveProcessQueryFormat = @"customMetrics
| where timestamp > ago({0}m)
| where cloud_RoleName != '' and cloud_RoleInstance != ''
| where name == 'Process CPU' or name == 'Cpu usage' or name == 'Gpu usage'
| extend ProcessName = tostring(customDimensions['Process Name'])
| extend PoolName = cloud_RoleName, ComputeNodeName = cloud_RoleInstance
| extend SampleAvg = value / iff(isnull(valueCount), 1, valueCount)
| summarize CpuAvg = avg(SampleAvg) by PoolName, ComputeNodeName, name, ProcessName, bin(timestamp, 5m)
";

        private readonly IAppInsightsQueryProvider _queryProvider;

        public ActiveNodeProvider(IAppInsightsQueryProvider queryProvider)
        {
            _queryProvider = queryProvider;
        }

        public async Task<List<ActiveComputeNode>> GetActiveComputeNodes(RenderingEnvironment config)
        {
            var activeNodes = new List<ActiveComputeNode>();
            var query = string.Format(ActiveProcessQueryFormat, 120);
            var result = await _queryProvider.ExecuteQuery(
                config.ApplicationInsightsAccount.ApplicationId,
                config.ApplicationInsightsAccount.ApiKey,
                query);

            if (!result.Success)
            {
                return activeNodes;
            }

            var json = result.Results;

            if (json.ContainsKey("tables"))
            {
                var tables = (JArray)json["tables"];
                if (tables.Count > 0)
                {
                    var rows = (JArray)tables[0]["rows"];
                    foreach (var row in rows)
                    {
                        var acn = new ActiveComputeNode
                        {
                            PoolName = row[0].Value<string>(),
                            ComputeNodeName = row[1].Value<string>(),
                            TrackedProcess = !string.IsNullOrEmpty(row[3].Value<string>()),
                            LastActive = row[4].Value<DateTime>(),
                        };

                        var metric = row[2].Value<string>();
                        if (!string.IsNullOrEmpty(metric))
                        {
                            if (metric == "Cpu usage")
                            {
                                acn.CpuPercent = row[5].Value<long>();
                            }
                            else if (metric == "Gpu usage")
                            {
                                acn.GpuPercent = row[5].Value<long>();
                            }
                        }

                        activeNodes.Add(acn);
                    }
                }
            }

            return activeNodes;
        }
    }
}