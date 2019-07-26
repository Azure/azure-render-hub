using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRenderHub.WebApp.Providers.Logs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Client;
using WebApp.Code.Attributes;
using WebApp.Code.Contract;
using WebApp.Controllers;

namespace AzureRenderHub.WebApp.Controllers
{
    [MenuActionFilter]
    public class LoggingController : MenuBaseController
    {
        private readonly ILogProvider _logProvider;

        public LoggingController(
            IEnvironmentCoordinator environmentCoordinator,
            IPackageCoordinator packageCoordinator,
            IAssetRepoCoordinator assetRepoCoordinator,
            ILogProvider logProvider,
            ITokenAcquisition tokenAcquisition) : base(
                environmentCoordinator,
                packageCoordinator,
                assetRepoCoordinator,
                tokenAcquisition)
        {
            _logProvider = logProvider;
        }

        [HttpGet]
        [Route("Logs")]
        public async Task<ActionResult> Index()
        {
            return View();
        }
    }
}
