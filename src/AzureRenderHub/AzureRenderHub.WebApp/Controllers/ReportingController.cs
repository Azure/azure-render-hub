// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using AzureRenderHub.WebApp.Models.Reporting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Client;
using WebApp.Code.Attributes;
using WebApp.Code.Contract;
using WebApp.CostManagement;
using WebApp.Models.Reporting;

namespace WebApp.Controllers
{
    [MenuActionFilter]
    [EnvironmentsActionFilter]
    public class ReportingController : MenuBaseController
    {
        private readonly ICostCoordinator _costCoordinator;

        public ReportingController(
            IEnvironmentCoordinator environmentCoordinator,
            IPackageCoordinator packageCoordinator,
            IAssetRepoCoordinator assetRepoCoordinator,
            ICostCoordinator costCoordinator,
            ITokenAcquisition tokenAcquisition)
            : base(environmentCoordinator, packageCoordinator, assetRepoCoordinator, tokenAcquisition)
        {
            _costCoordinator = costCoordinator;
        }

        [HttpGet]
        [Route("Reporting", Name = nameof(Index))]
        public async Task<ActionResult> Index([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
        {
            var envs = (await Environments()).Where(env => !env.InProgress);

            var period = new CostPeriod(nameof(Index), Url, from, to);

            var nextMonthLink = period.GetNextMonthLink();
            var currentMonthLink = period.GetCurrentMonthLink();
            var prevMonthLink = period.GetPrevMonthLink();

            return View(
                new IndexModel(
                    period.QueryTimePeriod.From,
                    period.QueryTimePeriod.To,
                    envs,
                    nextMonthLink,
                    currentMonthLink,
                    prevMonthLink));
        }
    }
}
