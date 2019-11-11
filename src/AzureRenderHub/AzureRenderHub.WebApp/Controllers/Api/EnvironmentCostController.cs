// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApp.Code.Contract;
using WebApp.CostManagement;

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

        [HttpGet("api/environments/{environmentName}/costs")]
        public async Task<ActionResult> EnvironmentCosts(string environmentName)
        {
            var environment = await _environmentCoordinator.GetEnvironment(environmentName);
            if (environment == null)
            {
                return NotFound();
            }

            var cost = await _costCoordinator.GetCost(environment, ReportingController.GetQueryPeriod(from: null, to: null));

            return Ok(cost);
        }
    }
}
