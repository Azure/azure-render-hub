// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TaskTupleAwaiter;
using WebApp.Code.Attributes;
using WebApp.Code.Contract;
using WebApp.Config;
using WebApp.CostManagement;

using static System.FormattableString;

namespace WebApp.Controllers
{
    [MenuActionFilter]
    [EnvironmentsActionFilter]
    public class ReportingController : MenuBaseController
    {
        private static readonly TimeSpan CacheResultsFor = TimeSpan.FromMinutes(10);

        private readonly CostManagementClientAccessor _clientAccessor;
        private readonly IMemoryCache _memoryCache;

        public ReportingController(
            IEnvironmentCoordinator environmentCoordinator,
            IPackageCoordinator packageCoordinator,
            IAssetRepoCoordinator assetRepoCoordinator,
            CostManagementClientAccessor clientAccessor,
            IMemoryCache memoryCache)
            : base(environmentCoordinator, packageCoordinator, assetRepoCoordinator)
        {
            _clientAccessor = clientAccessor;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        [Route("Reporting", Name = nameof(Index))]
        public async Task<ActionResult> Index([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
        {
            var (envs, client) = await (Environments(), _clientAccessor.GetClient());

            var period = GetQueryPeriod(from, to);

            var usages = await Task.WhenAll(envs.Select(env => GetUsage(client, env, period)));

            var nextMonthLink = GetNextMonthLink(period);
            var prevMonthLink = GetPrevMonthLink(period);

            return View(new IndexModel(usages, nextMonthLink, prevMonthLink));
        }

        public class IndexModel
        {
            public IndexModel(
                (string env, UsageResponse usage)[] usages,
                string nextMonth,
                string prevMonth)
            {
                UsagePerEnvironment = new SortedDictionary<string, UsageResponse>(usages.ToDictionary(pair => pair.env, pair => pair.usage));
                NextMonthLink = nextMonth;
                PreviousMonthLink = prevMonth;
            }

            public SortedDictionary<string, UsageResponse> UsagePerEnvironment { get; }

            public string NextMonthLink { get; }

            public string PreviousMonthLink { get; }

        }

        public string GetPrevMonthLink(QueryTimePeriod period)
            => Url.RouteUrl(nameof(Index), new { from = PrevMonthStart(period.From), to = PrevMonthEnd(period.From) });

        public string GetNextMonthLink(QueryTimePeriod period)
        {
            var now = DateTimeOffset.UtcNow;
            if (period.To >= now)
            {
                return null;
            }

            return Url.RouteUrl(nameof(Index), new { from = NextMonthStart(period.To), to = NextMonthEnd(period.To) });
        }

        public static QueryTimePeriod GetQueryPeriod(DateTimeOffset? from, DateTimeOffset? to)
        {
            if (from == null && to == null)
            {
                return ThisMonth();
            }
            else if (to == null)
            {
                return new QueryTimePeriod(from: from.Value, to: EndOfMonth(from.Value));
            }
            else if (from == null)
            {
                return new QueryTimePeriod(from: StartOfMonth(to.Value), to: to.Value);
            }
            else
            {
                return new QueryTimePeriod(from: from.Value, to: to.Value);
            }
        }

        public static DateTimeOffset NextMonthStart(DateTimeOffset value)
            => StartOfMonth(value).AddMonths(1);

        public static DateTimeOffset NextMonthEnd(DateTimeOffset value)
            => EndOfMonth(NextMonthStart(value));

        public static DateTimeOffset PrevMonthStart(DateTimeOffset value)
            => StartOfMonth(value).AddMonths(-1);

        public static DateTimeOffset PrevMonthEnd(DateTimeOffset value)
            => EndOfMonth(PrevMonthStart(value));

        public static DateTimeOffset StartOfMonth(DateTimeOffset value)
            => new DateTimeOffset(value.Year, value.Month, 1, 0, 0, 0, value.Offset);

        public static DateTimeOffset EndOfMonth(DateTimeOffset value)
            => NextMonthStart(value) - TimeSpan.FromSeconds(1);

        public static QueryTimePeriod ThisMonth()
        {
            var today = DateTimeOffset.UtcNow;
            return new QueryTimePeriod(from: StartOfMonth(today), to: EndOfMonth(today));
        }

        private async Task<(string envName, UsageResponse)> GetUsage(
            CostManagementClient client,
            RenderingEnvironment env,
            QueryTimePeriod timePeriod)
        {
            var cacheKey = Invariant($"REPORTING:{env.Name}/{timePeriod.From}/{timePeriod.To}");

            var usageResponse = await _memoryCache.GetOrCreateAsync(cacheKey, Fetch); 
            return (env.Name, usageResponse);

            Task<UsageResponse> Fetch(ICacheEntry ice)
            {
                ice.AbsoluteExpirationRelativeToNow = CacheResultsFor;
                
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

                usageRequest.TimePeriod = timePeriod;

                return client.GetUsageForSubscription(env.SubscriptionId, usageRequest);
            }
        }
    }
}
