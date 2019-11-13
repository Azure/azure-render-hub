// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            var period = GetQueryPeriod(from, to);

            var usages = await Task.WhenAll(envs
                .Where(env => !env.InProgress)
                .Select(env => _costCoordinator.GetCost(env, period)));

            var squishedCosts = usages?.Where(u => u.Cost != null).Select(u => u.Cost.Recategorize(u.EnvironmentId));

            var summaryCost = CalculateSummarySafely(squishedCosts);

            var nextMonthLink = GetNextMonthLink(period, nameof(AllCosts));
            var currentMonthLink = GetCurrentMonthLink(nameof(AllCosts));
            var prevMonthLink = GetPrevMonthLink(period, nameof(AllCosts));

            return Ok(new EnvironmentCost("Total Costs", summaryCost)
            {
                CurrentMonthLink = currentMonthLink,
                NextMonthLink = nextMonthLink,
                PreviousMonthLink = prevMonthLink,
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

            var period = GetQueryPeriod(from, to);
            var nextMonthLink = GetNextMonthLink(period, nameof(EnvironmentCosts));
            var currentMonthLink = GetCurrentMonthLink(nameof(EnvironmentCosts));
            var prevMonthLink = GetPrevMonthLink(period, nameof(EnvironmentCosts));

            var cost = await _costCoordinator.GetCost(environment, ReportingController.GetQueryPeriod(from: from, to: to));

            cost.CurrentMonthLink = currentMonthLink;
            cost.NextMonthLink = nextMonthLink;
            cost.PreviousMonthLink = prevMonthLink;

            return Ok(cost);
        }

        private async Task<IReadOnlyList<RenderingEnvironment>> Environments()
        {
            var envs = await Task.WhenAll((await _environmentCoordinator.ListEnvironments())
                .Select(env => _environmentCoordinator.GetEnvironment(env)));

            return envs.Where(re => re != null).OrderBy(re => re.Name).ToList();
        }

        private string GetCurrentMonthLink(string routeName)
        {
            var thisMonth = ThisMonth();
            return Url.RouteUrl(routeName, new { from = thisMonth.From, to = thisMonth.To });
        }

        private string GetPrevMonthLink(QueryTimePeriod period, string routeName)
            => Url.RouteUrl(routeName, new { from = PrevMonthStart(period.From), to = PrevMonthEnd(period.From) });

        private string GetNextMonthLink(QueryTimePeriod period, string routeName)
        {
            var now = DateTimeOffset.UtcNow;
            if (period.To >= now)
            {
                return null;
            }

            return Url.RouteUrl(routeName, new { from = NextMonthStart(period.To), to = NextMonthEnd(period.To) });
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

        private static QueryTimePeriod GetQueryPeriod(DateTimeOffset? from, DateTimeOffset? to)
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

        private static DateTimeOffset NextMonthStart(DateTimeOffset value)
            => StartOfMonth(value).AddMonths(1);

        private static DateTimeOffset NextMonthEnd(DateTimeOffset value)
            => EndOfMonth(NextMonthStart(value));

        private static DateTimeOffset PrevMonthStart(DateTimeOffset value)
            => StartOfMonth(value).AddMonths(-1);

        private static DateTimeOffset PrevMonthEnd(DateTimeOffset value)
            => EndOfMonth(PrevMonthStart(value));

        private static DateTimeOffset StartOfMonth(DateTimeOffset value)
            => new DateTimeOffset(value.Year, value.Month, 1, 0, 0, 0, value.Offset);

        private static DateTimeOffset EndOfMonth(DateTimeOffset value)
            => NextMonthStart(value) - TimeSpan.FromSeconds(1);

        private static QueryTimePeriod ThisMonth()
        {
            var today = DateTimeOffset.UtcNow;
            return new QueryTimePeriod(from: StartOfMonth(today), to: EndOfMonth(today));
        }
    }
}
