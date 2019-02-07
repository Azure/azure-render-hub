// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Blob;
using WebApp.Code;
using WebApp.Code.Attributes;
using WebApp.Code.Contract;
using WebApp.Config;
using WebApp.Models.Packages;

namespace WebApp.Controllers
{
    [MenuActionFilter]
    [PackagesActionFilter]
    public class PackagesController : MenuBaseController
    {
        private readonly IHostingEnvironment _environment;
        private readonly CloudBlobClient _blobClient;
        private readonly IPackageCoordinator _packageCoordinator;

        public PackagesController(
            IHostingEnvironment environment,
            CloudBlobClient cloudBlobClient,
            IEnvironmentCoordinator environmentCoordinator,
            IPackageCoordinator packageCoordinator,
            IAssetRepoCoordinator assetRepoCoordinator) : base(environmentCoordinator, packageCoordinator, assetRepoCoordinator)
        {
            _environment = environment;
            _blobClient = cloudBlobClient;
            _packageCoordinator = packageCoordinator;
        }

        [HttpGet]
        [Route("RenderManagerPackages")]
        public async Task<ActionResult> Index()
        {
            var packages = await Task.WhenAll((await _packageCoordinator.ListPackages())
                .Select(packageName => _packageCoordinator.GetPackage(packageName)));
            return View(new ListPackagesModel { Packages = packages.ToList() });
        }

        [HttpDelete]
        [Route("RenderManagerPackages/Delete/{pkgId}")]
        public async Task<ActionResult> Delete(string pkgId)
        {
            var package = await _packageCoordinator.GetPackage(pkgId);
            if (package == null)
            {
                return NotFound("Package not found");
            }

            var container = _blobClient.GetContainerReference(package.Container);
            if (container == null)
            {
                return NotFound("Storage container not found");
            }

            if (!await container.DeleteIfExistsAsync())
            {
                return StatusCode(500, "Container was unable to be deleted");
            }

            if (await _packageCoordinator.RemovePackage(package))
            {
                return Ok();
            }

            return StatusCode(500, "Unable to remove package");
        }

        [HttpGet]
        [Route("RenderManagerPackages/{pkgId}/Details")]
        public async Task<IActionResult> Details(string pkgId)
        {
            var package = await _packageCoordinator.GetPackage(pkgId);
            if (package == null)
            {
                return RedirectToAction("Add", new { pkgId });
            }

            var model = new ViewPackageModel(package);
            return View(model);
        }

        [HttpGet]
        [Route("RenderManagerPackages/Add/{pkgId?}")]
        public ActionResult Add(string pkgId)
        {
            var model = new AddPackageModel { PackageName = pkgId };
            return View(model);
        }

        [HttpPost]
        [RequestSizeLimit(Constants.MaxRequestSizeLimit)]
        public async Task<ActionResult> AddQube610(AddPackageModel model)
        {
            return await AddQube(model.QubePackage, InstallationPackageType.Qube610);
        }

        [HttpPost]
        [RequestSizeLimit(Constants.MaxRequestSizeLimit)]
        public async Task<ActionResult> AddQube70(AddPackageModel model)
        {
            return await AddQube(model.QubePackage, InstallationPackageType.Qube70);
        }

        [HttpPost]
        [RequestSizeLimit(Constants.MaxRequestSizeLimit)]
        public async Task<ActionResult> AddDeadline10(AddPackageModel model)
        {
            return await AddGeneralPackageImpl(model.GeneralPackage, InstallationPackageType.Deadline10);
        }

        [HttpPost]
        [RequestSizeLimit(Constants.MaxRequestSizeLimit)]
        public async Task<ActionResult> AddGpu(AddPackageModel model)
        {
            return await AddGeneralPackageImpl(model.GeneralPackage, InstallationPackageType.Gpu);
        }

        [HttpPost]
        [RequestSizeLimit(Constants.MaxRequestSizeLimit)]
        public async Task<ActionResult> AddGeneral(AddPackageModel model)
        {
            return await AddGeneralPackageImpl(model.GeneralPackage, InstallationPackageType.General);
        }

        private async Task<ActionResult> AddGeneralPackageImpl(AddGeneralPackageModel model, InstallationPackageType type)
        {
            if (!ModelState.IsValid)
            {
                return View("Add", new AddPackageModel
                {
                    PackageName = model.PackageName,
                    GeneralPackage = model,
                });
            }

            try
            {
                await CreatePackage(
                    model.PackageName,
                    type,
                    model.Files,
                    model.InstallCommandLine);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Failed to completely create package with error: {ex}");
                return View("Add", new AddPackageModel
                {
                    PackageName = model.PackageName,
                    GeneralPackage = model,
                });
            }

            return RedirectToAction("Details", new { pkgId = model.PackageName });
        }

        private async Task<ActionResult> AddQube(AddQubePackageModel model, InstallationPackageType type)
        {
            if (!ModelState.IsValid)
            {
                return View("Add", new AddPackageModel
                {
                    PackageName = model.PackageName,
                    QubePackage = model,
                });
            }

            try
            {
                var files = new List<IFormFile>(new[]
                {
                    model.PythonInstaller,
                    model.QbConf,
                    model.QubeCoreMsi,
                    model.QubeWorkerMsi
                });

                files.AddRange(model.QubeJobTypeMsis);

                await CreatePackage(
                    model.PackageName,
                    type,
                    files,
                    model.InstallCommandLine);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Failed to completely create package with error: {ex}");
                return View("Add", new AddPackageModel
                {
                    PackageName = model.PackageName,
                    QubePackage = model,
                });
            }

            return RedirectToAction("Details", new { pkgId = model.PackageName });
        }

        private async Task CreatePackage(
            string packageId,
            InstallationPackageType type,
            IEnumerable<IFormFile> files,
            string commandLine = null)
        {
            var package = await _packageCoordinator.GetPackage(packageId)
                ?? new InstallationPackage(packageId, type);

            try
            {
                var container = _blobClient.GetContainerReference(package.Container);
                if (container != null)
                {
                    await container.CreateIfNotExistsAsync();
                }

                // Sanitise files as it can contain nulls
                files = files.Where(f => f != null);

                foreach (var file in files)
                {
                    await UploadFileToBlob(file, container);
                }

                package.Files.Clear();
                package.Files.AddRange(files.Select(f => f.FileName));
                package.PackageInstallCommand = commandLine;

                await _packageCoordinator.UpdatePackage(package);
            }
            catch (AggregateException aex)
            {
                throw aex.InnerExceptions.First();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task UploadFileToBlob(IFormFile file, CloudBlobContainer container)
        {
            var blob = container.GetBlockBlobReference(file.FileName);
            using (var stream = file.OpenReadStream())
            {
                await blob.UploadFromStreamAsync(stream);
            }
        }
    }
}
