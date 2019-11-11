// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApp.AppInsights.PoolUsage;
using WebApp.Code.Contract;

namespace WebApp.Controllers.Api
{
    [ApiController] // performs model validation automatically
    [RequireHttps]
    [Authorize]
    public class PoolUsageController : Controller
    {
        private readonly IEnvironmentCoordinator _environmentCoordinator;
        private readonly IPoolUsageProvider _poolUsageProvider;
        private readonly ILogger<PoolUsageController> _logger;

        public PoolUsageController(
            IEnvironmentCoordinator environmentCoordinator,
            IPoolUsageProvider poolUsageProvider,
            ILogger<PoolUsageController> logger)
        {
            _environmentCoordinator = environmentCoordinator;
            _poolUsageProvider = poolUsageProvider;
            _logger = logger;
        }

        [HttpGet("api/environments/{environmentName}/poolUsage")]
        public async Task<ActionResult> EnvironmentPoolsUsage(string environmentName)
        {
            var environment = await _environmentCoordinator.GetEnvironment(environmentName);
            if (environment == null)
            {
                return NotFound();
            }

            var usage = await _poolUsageProvider.GetEnvironmentUsage(environment);

            return Ok(usage);
        }
    }
}
