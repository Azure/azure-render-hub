// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly CloudBlobClient _blobClient;
        private readonly IPackageCoordinator _packageCoordinator;

        public PackagesController(
            CloudBlobClient cloudBlobClient,
            IEnvironmentCoordinator environmentCoordinator,
            IPackageCoordinator packageCoordinator,
            IAssetRepoCoordinator assetRepoCoordinator) : base(environmentCoordinator, packageCoordinator, assetRepoCoordinator)
        {
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
        [Route("RenderManagerPackages/{pkgId}/Delete")]
        public async Task<ActionResult> Delete(string pkgId)
        {
            var package = await _packageCoordinator.GetPackage(pkgId);
            if (package == null)
            {
                return NotFound("Package not found");
            }

            var container = _blobClient.GetContainerReference(package.Container);
            await container.DeleteIfExistsAsync();
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
        [Route("RenderManagerPackages/Add")]
        public ActionResult Add(string pkgId)
        {
            var model = new AddPackageModel { PackageName = pkgId };
            return View(model);
        }

        [HttpGet]
        [Route("RenderManagerPackages/{pkgId}/Edit")]
        public async Task<ActionResult> Edit(string pkgId)
        {
            var package = await _packageCoordinator.GetPackage(pkgId);
            if (package == null)
            {
                return RedirectToAction("Index");
            }

            var model = new EditPackageModel
            {
                PackageName = pkgId,
                InstallCommandLine = package.PackageInstallCommand,
            };

            return View(model);
        }

        [HttpPost]
        [Route("RenderManagerPackages/{pkgId}/Edit")]
        public async Task<ActionResult> Edit(string pkgId, EditPackageModel model)
        {
            var package = await _packageCoordinator.GetPackage(pkgId);
            if (package == null)
            {
                return RedirectToAction("Index");
            }

            package.PackageInstallCommand = model.InstallCommandLine;
            await _packageCoordinator.UpdatePackage(package);

            return RedirectToAction("Details", new {pkgId});
        }

        [HttpPost]
        [RequestSizeLimit(Constants.MaxRequestSizeLimit)]
        public async Task<ActionResult> AddQube610(AddPackageModel model)
        {
            return await AddQube(model.PackageName, model.QubePackage, InstallationPackageType.Qube610);
        }

        [HttpPost]
        [RequestSizeLimit(Constants.MaxRequestSizeLimit)]
        public async Task<ActionResult> AddQube70(AddPackageModel model)
        {
            return await AddQube(model.PackageName, model.QubePackage, InstallationPackageType.Qube70);
        }

        [HttpPost]
        [RequestSizeLimit(Constants.MaxRequestSizeLimit)]
        public async Task<ActionResult> AddDeadline10(AddPackageModel model)
        {
            return await AddGeneralPackageImpl(model.PackageName, model.GeneralPackage, InstallationPackageType.Deadline10);
        }

        [HttpPost]
        [RequestSizeLimit(Constants.MaxRequestSizeLimit)]
        public async Task<ActionResult> AddGpu(AddPackageModel model)
        {
            return await AddGeneralPackageImpl(model.PackageName, model.GeneralPackage, InstallationPackageType.Gpu);
        }

        [HttpPost]
        [RequestSizeLimit(Constants.MaxRequestSizeLimit)]
        public async Task<ActionResult> AddGeneral(AddPackageModel model)
        {
            return await AddGeneralPackageImpl(model.PackageName, model.GeneralPackage, InstallationPackageType.General);
        }

        private async Task<ActionResult> AddGeneralPackageImpl(string packageName, AddGeneralPackageModel model, InstallationPackageType type)
        {
            if (model.Files == null || !model.Files.Any())
            {
                ModelState.AddModelError("GeneralPackage.Files", $"At least one file must be specified.");
            }

            if (!ModelState.IsValid)
            {
                return View("Add", new AddPackageModel
                {
                    PackageName = packageName,
                    GeneralPackage = model,
                    Type = type,
                });
            }

            try
            {
                await CreatePackage(
                    packageName,
                    type,
                    model.Files,
                    model.InstallCommandLine);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Failed to completely create package with error: {ex}");
                return View("Add", new AddPackageModel
                {
                    PackageName = packageName,
                    GeneralPackage = model,
                    Type = type,
                });
            }

            return RedirectToAction("Details", new { pkgId = packageName });
        }

        private async Task<ActionResult> AddQube(string packageName, AddQubePackageModel model, InstallationPackageType type)
        {
            if (model.PythonInstaller == null)
            {
                ModelState.AddModelError("QubePackage.PythonInstaller", $"Python installer must be specified.");
            }

            if (model.QubeCoreMsi == null)
            {
                ModelState.AddModelError("QubePackage.QubeCoreMsi", $"Qube Core installer must be specified.");
            }

            if (model.PythonInstaller == null)
            {
                ModelState.AddModelError("QubePackage.QubeWorkerMsi", $"Qube Worker installer must be specified.");
            }

            if (!ModelState.IsValid)
            {
                return View("Add", new AddPackageModel
                {
                    PackageName = packageName,
                    QubePackage = model,
                    Type = type,
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
                    packageName,
                    type,
                    files,
                    model.InstallCommandLine);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Failed to completely create package with error: {ex}");
                return View("Add", new AddPackageModel
                {
                    PackageName = packageName,
                    QubePackage = model,
                    Type = type,
                });
            }

            return RedirectToAction("Details", new { pkgId = packageName });
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
