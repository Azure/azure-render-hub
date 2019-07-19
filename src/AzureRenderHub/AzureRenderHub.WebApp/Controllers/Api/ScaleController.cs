// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApp.Code.Attributes;
using WebApp.Code.Contract;
using WebApp.Providers.Resize;
using WebApp.Util;

namespace WebApp.Controllers.Api
{
    [ApiController] // performs model validation automatically
    [RequireHttps]
    [AuthorizeEnvironmentEndpoint]
    public class ScaleController : Controller
    {
        private readonly IEnvironmentCoordinator _environmentCoordinator;
        private readonly IScaleUpRequestStore _scaleUpStore;
        private readonly AsyncAutoResetEvent _trigger;
        private readonly ILogger<ScaleController> _logger;

        public ScaleController(
            IEnvironmentCoordinator environmentCoordinator,
            IScaleUpRequestStore scaleUpStore,
            AsyncAutoResetEvent trigger,
            ILogger<ScaleController> logger)
        {
            _environmentCoordinator = environmentCoordinator;
            _scaleUpStore = scaleUpStore;
            _trigger = trigger;
            _logger = logger;
        }

        [HttpPost("api/environments/{environmentName}/pools/{poolName}")]
        public async Task<ActionResult> ScaleUpPool(string environmentName, string poolName, [FromBody] Models.Api.ScaleUpRequest request)
        {
            var environment = await _environmentCoordinator.GetEnvironment(environmentName);
            if (environment == null)
            {
                return NotFound();
            }

            // TODO: should we validate the pool here as well?

            try
            {
                await _scaleUpStore.Add(
                    environment.Name,
                    poolName,
                    request.RequestedNodes);

                // tell scale up processor to start now, bypass delay
                _trigger.Set();

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to add scale entry to table storage for environment: {environmentName} and pool: {poolName}");
                throw;
            }
        }
    }
}
