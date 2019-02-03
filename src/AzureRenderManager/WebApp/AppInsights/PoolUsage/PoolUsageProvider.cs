// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebApp.Config;
using WebApp.Operations;

namespace WebApp.AppInsights.PoolUsage
{
    public class PoolUsageProvider : IPoolUsageProvider
    {
        private const string EnvironmentUsage = @"
customMetrics
| where timestamp > ago(7d)
| extend poolName = cloud_RoleName
| where poolName != """"
| summarize coreCount = dcountif(strcat(cloud_RoleInstance, tostring(customDimensions[""CPU #""])), customDimensions[""CPU #""] != """"), nodeCount = dcount(cloud_RoleInstance) by poolName, bin(timestamp, 15m)
| project timestamp, poolName, nodeCount, coreCount
| order by timestamp asc
";

        private const string PoolUsage = @"
customMetrics
| where timestamp > ago(7d)
| extend poolName = cloud_RoleName
| where poolName != """"
| extend isLowPriority = iif(cloud_RoleInstance endswith 'p', true, false)
| where poolName == ""{0}""
| summarize coresPerNode = dcountif(tostring(customDimensions[""CPU #""]), customDimensions[""CPU #""] != """"), dedicatedNodes = dcountif(cloud_RoleInstance, isLowPriority == false), lowPriorityNodes = dcountif(cloud_RoleInstance, isLowPriority == true) by poolName, bin(timestamp, 15m)
| project timestamp, dedicatedNodes, dedicatedCores = dedicatedNodes * coresPerNode, lowPriorityNodes, lowPriorityCores = lowPriorityNodes * coresPerNode
| order by timestamp asc
";

        private readonly IAppInsightsQueryProvider _queryProvider;

        public PoolUsageProvider(IAppInsightsQueryProvider queryProvider)
        {
            _queryProvider = queryProvider;
        }

        public async Task<IList<PoolUsageResult>> GetEnvironmentUsage(RenderingEnvironment environment)
        {
            var result = await _queryProvider.ExecuteQuery(
                environment.ApplicationInsightsAccount.ApplicationId,
                environment.ApplicationInsightsAccount.ApiKey,
                EnvironmentUsage);

            var usage = new List<PoolUsageResult>();

            if (result.Success)
            {
                var values = GetEnvironmentUsageMetrics(result.Results);
                var poolNames = values.Select(p => p.PoolName).Distinct().ToList();
                foreach (var poolName in poolNames)
                {
                    var poolUsage = new PoolUsageResult {PoolName = poolName};
                    poolUsage.Values = values.Where(p => p.PoolName == poolName).OrderBy(p => p.Timestamp).ToList();
                    usage.Add(poolUsage);
                }
            }
            else
            {
                Console.WriteLine($"Error querying environment {environment.Name} usage: {result.Error}");
            }

            return usage;
        }

        public async Task<PoolUsageResult> GetUsageForPool(RenderingEnvironment environment, string poolName, string vmSize)
        {
            var result = await _queryProvider.ExecuteQuery(
                environment.ApplicationInsightsAccount.ApplicationId,
                environment.ApplicationInsightsAccount.ApiKey,
                GetPoolUsageQuery(poolName));

            var usage = new PoolUsageResult { PoolName = poolName };

            if (result.Success)
            {
                usage.Values = GetPoolUsageMetrics(result.Results);
            }
            else
            {
                Console.WriteLine($"Error querying pool {poolName} usage: {result.Error}");
            }

            return usage;
        }

        private string GetPoolUsageQuery(string poolName)
        {
            return string.Format(PoolUsage, poolName);
        }

        private IList<PoolUsageMetric> GetPoolUsageMetrics(JObject json)
        {
            var metrics = new List<PoolUsageMetric>();

            if (json.ContainsKey("tables"))
            {
                var tables = (JArray)json["tables"];
                if (tables.Count > 0)
                {
                    var rows = (JArray)tables[0]["rows"];
                    foreach (var row in rows)
                    {
                        metrics.Add(new PoolUsageMetric
                        {
                            Timestamp = row[0].Value<DateTime>(),
                            DedicatedNodes = (int)row[1].Value<long>(),
                            DedicatedCores = (int)row[2].Value<long>(),
                            LowPriorityNodes = (int)row[3].Value<long>(),
                            LowPriorityCores = (int)row[4].Value<long>(),
                        });
                    }
                }
            }

            return metrics;
        }

        private IList<PoolUsageMetric> GetEnvironmentUsageMetrics(JObject json)
        {
            var metrics = new List<PoolUsageMetric>();

            if (json.ContainsKey("tables"))
            {
                var tables = (JArray)json["tables"];
                if (tables.Count > 0)
                {
                    var rows = (JArray)tables[0]["rows"];
                    foreach (var row in rows)
                    {
                        metrics.Add(new PoolUsageMetric
                        {
                            Timestamp = row[0].Value<DateTime>(),
                            PoolName = row[1].Value<string>(),
                            TotalNodes = (int)row[2].Value<long>(),
                            TotalCores = (int)row[3].Value<long>(),
                        });
                    }
                }
            }

            return metrics;
        }
    }
}
