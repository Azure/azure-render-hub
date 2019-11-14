// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRenderHub.WebApp.Models.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApp.Code.Contract;
using WebApp.Config;
using WebApp.CostManagement;
using WebApp.Models.Reporting;

namespace WebApp.Controllers.Api
{
    [ApiController] // performs model validation automatically
    [RequireHttps]
    [Authorize]
    public class EnvironmentCostController : Controller
    {
        private readonly IEnvironmentCoordinator _environmentCoordinator;
        private readonly ICostCoordinator _costCoordinator;
        private readonly ILogger<EnvironmentCostController> _logger;

        public EnvironmentCostController(
            IEnvironmentCoordinator environmentCoordinator,
            ICostCoordinator costCoordinator,
            ILogger<EnvironmentCostController> logger)
        {
            _environmentCoordinator = environmentCoordinator;
            _costCoordinator = costCoordinator;
            _logger = logger;
        }

        [HttpGet("api/reporting/costs")]
        public async Task<ActionResult> AllCosts([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
        {
            var envs = (await Environments()).Where(env => !env.InProgress);

            var period = new CostPeriod(nameof(AllCosts), Url, from, to);

            var usages = await Task.WhenAll(envs
                .Where(env => !env.InProgress)
                .Select(env => _costCoordinator.GetCost(env, period.QueryTimePeriod)));

            var squishedCosts = usages?.Where(u => u.Cost != null).Select(u => u.Cost.Recategorize(u.EnvironmentId));

            var summaryCost = CalculateSummarySafely(squishedCosts);

            return Ok(new EnvironmentCost("Total Costs", summaryCost)
            {
                CurrentMonthLink = period.GetCurrentMonthLink(),
                NextMonthLink = period.GetNextMonthLink(),
                PreviousMonthLink = period.GetPrevMonthLink(),
            });
        }

        [HttpGet("api/environments/{environmentName}/costs")]
        public async Task<ActionResult> EnvironmentCosts(string environmentName, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
        {
            var environment = await _environmentCoordinator.GetEnvironment(environmentName);
            if (environment == null)
            {
                return NotFound();
            }

            var period = new CostPeriod(nameof(EnvironmentCosts), Url, from, to);

            var cost = await _costCoordinator.GetCost(environment, period.QueryTimePeriod);

            cost.CurrentMonthLink = period.GetCurrentMonthLink();
            cost.NextMonthLink = period.GetNextMonthLink();
            cost.PreviousMonthLink = period.GetPrevMonthLink();

            return Ok(cost);
        }

        private async Task<IReadOnlyList<RenderingEnvironment>> Environments()
        {
            var envs = await Task.WhenAll((await _environmentCoordinator.ListEnvironments())
                .Select(env => _environmentCoordinator.GetEnvironment(env)));

            return envs.Where(re => re != null).OrderBy(re => re.Name).ToList();
        }

        private Cost CalculateSummarySafely(IEnumerable<Cost> squishedCosts)
        {
            try
            {
                return squishedCosts.Any() ? squishedCosts.Aggregate(Cost.Aggregate) : null;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
