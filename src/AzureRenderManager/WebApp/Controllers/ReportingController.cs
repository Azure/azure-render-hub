// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TaskTupleAwaiter;
using WebApp.Code.Attributes;
using WebApp.Code.Contract;
using WebApp.Config;
using WebApp.CostManagement;

namespace WebApp.Controllers
{
    [MenuActionFilter]
    [EnvironmentsActionFilter]
    public class ReportingController : MenuBaseController
    {
        private readonly CostManagementClientAccessor _clientAccessor;

        public ReportingController(
            IEnvironmentCoordinator environmentCoordinator,
            IPackageCoordinator packageCoordinator,
            IAssetRepoCoordinator assetRepoCoordinator,
            CostManagementClientAccessor clientAccessor)
            : base(environmentCoordinator, packageCoordinator, assetRepoCoordinator)
        {
            _clientAccessor = clientAccessor;
        }

        [HttpGet]
        [Route("Reporting")]
        public async Task<ActionResult> Index()
        {
            var (envs, client) = await (Environments(), _clientAccessor.GetClient());

            var usages = await Task.WhenAll(envs.Select(env => GetUsage(client, env)));

            return View(usages);
        }

        private static async Task<(string envName, UsageResponse)> GetUsage(CostManagementClient client, RenderingEnvironment env)
        {
            var usageRequest =
                new UsageRequest(
                    Timeframe.MonthToDate,
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

            return (env.Name, await client.GetUsageForResourceGroup(env.SubscriptionId, env.ResourceGroupName, usageRequest));
        }
    }
}
