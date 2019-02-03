// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebApp.Code.Attributes;
using WebApp.Code.Contract;

namespace WebApp.Controllers
{
    [MenuActionFilter]
    [EnvironmentsActionFilter]
    public class ReportingController : MenuBaseController
    {
        public ReportingController(
            IEnvironmentCoordinator environmentCoordinator,
            IPackageCoordinator packageCoordinator,
            IAssetRepoCoordinator assetRepoCoordinator) :
            base(environmentCoordinator, packageCoordinator, assetRepoCoordinator)
        {
        }

        [HttpGet]
        [Route("Reporting")]
        public IActionResult Index() => View();
    }
}
