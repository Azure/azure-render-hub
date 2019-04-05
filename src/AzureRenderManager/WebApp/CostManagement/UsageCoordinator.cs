// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using WebApp.Config;
using WebApp.Models.Reporting;

using static System.FormattableString;

namespace WebApp.CostManagement
{
    public interface ICostCoordinator
    {
        Task<EnvironmentCost> GetCost(RenderingEnvironment renderingEnvironment, QueryTimePeriod period);
    }

    public class CachingCostCoordinator : ICostCoordinator
    {
        private static readonly TimeSpan CacheResultsFor = TimeSpan.FromMinutes(10);

        private readonly ICostCoordinator _inner;
        private readonly IMemoryCache _memoryCache;

        public CachingCostCoordinator(ICostCoordinator inner, IMemoryCache memoryCache)
        {
            _inner = inner;
            _memoryCache = memoryCache;
        }

        public Task<EnvironmentCost> GetCost(RenderingEnvironment env, QueryTimePeriod timePeriod)
        {
            var cacheKey = Invariant($"REPORTING:{env.Name}/{timePeriod.From}/{timePeriod.To}");

            return _memoryCache.GetOrCreateAsync(cacheKey, Fetch);

            Task<EnvironmentCost> Fetch(ICacheEntry ice)
            {
                ice.AbsoluteExpirationRelativeToNow = CacheResultsFor;

                return _inner.GetCost(env, timePeriod);
            }
        }
    }

    public class CostCoordinator : ICostCoordinator
    {
        private readonly CostManagementClientAccessor _clientAccessor;

        public CostCoordinator(CostManagementClientAccessor accessor)
        {
            _clientAccessor = accessor;
        }

        public async Task<EnvironmentCost> GetCost(RenderingEnvironment env, QueryTimePeriod period)
        {
            var client = await _clientAccessor.GetClient();
            var usageRequest = CreateUsageRequest(env, period);

            IReadOnlyList<Cost> costs =
                await Task.WhenAll(env.ExtractResourceGroupNames().Select(
                    async rgName =>
                    {
                        var result = await client.GetUsageForResourceGroup(env.SubscriptionId, rgName, usageRequest);

                        if (result.Properties == null)
                        {
                            return null;
                        }
                        else
                        {
                            return new Cost(usageRequest.TimePeriod, result);
                        }
                    }));

            costs = costs.Where(x => x != null).ToList();

            if (!costs.Any())
            {
                return new EnvironmentCost(env.Name, null);
            }

            return new EnvironmentCost(env.Name, costs.Aggregate((x, y) => new Cost(x, y)));
        }

        private static UsageRequest CreateUsageRequest(RenderingEnvironment env, QueryTimePeriod period)
        {
            var usageRequest =
                new UsageRequest(
                    Timeframe.Custom,
                    new Dataset(
                        Granularity.Daily,
                        new Dictionary<string, Aggregation>
                        {
                            { "totalCost", new Aggregation(AggregationFunction.Sum, "PreTaxCost") }
                        },
                        new List<Grouping>
                        {
                            new Grouping("MeterSubCategory", ColumnType.Dimension)
                        },
                        FilterExpression.Tag("environment", Operator.In, new[] { env.Id })));

            usageRequest.TimePeriod = period;

            return usageRequest;
        }
    }
}
