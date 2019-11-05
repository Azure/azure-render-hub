// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AzureRenderHub.WebApp.Code.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Azure.Management.Batch.Models;
using Microsoft.Identity.Web.Client;
using Microsoft.Rest.Azure;
using TaskTupleAwaiter;
using WebApp.AppInsights.PoolUsage;
using WebApp.Code;
using WebApp.Code.Attributes;
using WebApp.Code.Contract;
using WebApp.Code.Extensions;
using WebApp.Config;
using WebApp.Config.Pools;
using WebApp.Models.Pools;
using WebApp.Operations;
using WebApp.Util;

namespace WebApp.Controllers
{
    [MenuActionFilter]
    [EnvironmentsActionFilter]
    public class PoolsController : MenuBaseController, IEnvController
    {
        private const string NoPackageSelected = "--none--";

        private readonly IPoolCoordinator _poolCoordinator;
        private readonly IVMSizes _vmSizes;
        private readonly IPackageCoordinator _packageCoordinator;
        private readonly IPoolUsageProvider _poolUsageProvider;
        private readonly StartTaskProvider _startTaskProvider;

        public PoolsController(
            IPoolCoordinator poolCoordinator,
            IVMSizes vmSizes,
            IEnvironmentCoordinator environmentCoordinator,
            IPackageCoordinator packageCoordinator,
            IAssetRepoCoordinator assetRepoCoordinator,
            IPoolUsageProvider poolUsageProvider,
            ITokenAcquisition tokenAcquisition,
            StartTaskProvider startTaskProvider) : base(
                environmentCoordinator, 
                packageCoordinator, 
                assetRepoCoordinator,
                tokenAcquisition)
        {
            _poolCoordinator = poolCoordinator;
            _vmSizes = vmSizes;
            _packageCoordinator = packageCoordinator;
            _poolUsageProvider = poolUsageProvider;
            _startTaskProvider = startTaskProvider;
        }

        [HttpGet("Environments/{envId}/Pools")]
        public async Task<ActionResult<PoolListModel>> Index(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Index", "Environments");
            }

            var model = new PoolListModel
            {
                EnvironmentName = envId,
                BatchAccount = environment.BatchAccount.Name,
                Location = environment.BatchAccount.Location
            };

            try
            {
                var pools = (await _poolCoordinator.ListPools(environment)).Select(pool => new PoolListDetailsModel(pool));
                model.Pools.AddRange(pools);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Unable to list pools. Error: {ex}");
            }

            return View(model);
        }

        [HttpDelete("Environments/{envId}/Pools/{poolId}")]
        public async Task<ActionResult> Delete(string envId, string poolId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Index", "Environments");
            }

            try
            {
                await _poolCoordinator.DeletePool(environment, poolId);
                return Ok();
            }
            catch (CloudException cEx)
            {
                // TODO: Log somewhere
                if (cEx.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound();
                }

                return StatusCode(500, cEx);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex);
            }
        }

        [HttpGet("Environments/{envId}/Pools/Refresh")]
        public ActionResult Refresh(string envId)
        {
            _poolCoordinator.ClearCache(envId);
            return RedirectToAction("Index", new { envId });
        }

        [HttpGet("Environments/{envId}/Pools/{poolId}/Refresh")]
        public ActionResult Refresh(string envId, string poolId)
        {
            _poolCoordinator.ClearCache(envId);
            return RedirectToAction("Details", new { envId, poolId });
        }

        [HttpGet("Environments/{envId}/Pools/{poolId}")]
        public async Task<ActionResult<PoolConfigurationModel>> Overview(string envId, string poolId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Index", "Environments");
            }

            var pool = await _poolCoordinator.GetPool(environment, poolId);
            if (pool == null)
            {
                return NotFound();
            }

            var usage = await _poolUsageProvider.GetUsageForPool(environment, poolId, pool.VmSize);

            var model = new PoolDetailsModel(environment, pool, usage.Values);

            return View(model);
        }

        [HttpPost("Environments/{envId}/Pools/{poolId}")]
        public async Task<ActionResult<PoolDetailsModel>> Overview(string envId, string poolId, PoolDetailsModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Overview", model);
            }

            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Index", "Environments");
            }

            var pool = new Pool(name: poolId);

            pool.ScaleSettings = new ScaleSettings(
                new FixedScaleSettings(
                    targetDedicatedNodes: model.DedicatedNodes,
                    targetLowPriorityNodes: model.LowPriorityNodes));

            try
            {
                await _poolCoordinator.UpdatePool(environment, pool);
            }
            catch (CloudException cEx)
            {
                // TODO: Log somewhere
                // TODO: Handle .... StatusCode Conflict
                // The specified pool has an ongoing resize operation. RequestId: 6e080f7d-1765-4a7e-99b4-e76f003b599b
                Console.WriteLine(cEx);
                ModelState.AddModelError("", $"Failed to update the pool with error: {cEx}");

                return View("Overview", model);
            }

            return RedirectToAction("Overview", new {envId, poolId });
        }

        [HttpGet("Environments/{envId}/Pools/{poolId}/details")]
        public async Task<ActionResult<PoolConfigurationModel>> Details(string envId, string poolId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Index", "Environments");
            }

            var pool = await _poolCoordinator.GetPool(environment, poolId);
            if (pool == null)
            {
                return NotFound();
            }

            var model = new PoolDetailsModel(environment, pool);

            return View(model);
        }

        [HttpGet("Environments/{envId}/Pools/{poolId}/autoscale")]
        public async Task<ActionResult<PoolConfigurationModel>> AutoScale(string envId, string poolId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Index", "Environments");
            }

            var pool = await _poolCoordinator.GetPool(environment, poolId);
            if (pool == null)
            {
                return NotFound();
            }

            var model = new PoolDetailsModel(environment, pool);

            return View(model);
        }

        [HttpPost("Environments/{envId}/Pools/{poolId}/autoscale")]
        public async Task<ActionResult<PoolDetailsModel>> AutoScale(string envId, string poolId, PoolDetailsModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("AutoScale", model);
            }

            var environment = await _environmentCoordinator.GetEnvironment(model.EnvironmentName);
            if (environment == null)
            {
                return RedirectToAction("Index", "Environments");
            }

            try
            {
                var pool = await _poolCoordinator.GetPool(environment, poolId);
                var poolUpdate = new Pool(name: poolId, metadata: pool.Metadata);
                poolUpdate.Metadata.AddAutoScaleMetadata(model);
                await _poolCoordinator.UpdatePool(environment, pool);
            }
            catch (CloudException cEx)
            {
                // TODO: Log somewhere
                // TODO: Handle .... StatusCode Conflict
                // The specified pool has an ongoing resize operation. RequestId: 6e080f7d-1765-4a7e-99b4-e76f003b599b
                Console.WriteLine(cEx);
                ModelState.AddModelError("", $"Failed to update the pool with error: {cEx}");

                return View("AutoScale", model);
            }

            return RedirectToAction("AutoScale", new { envId = envId, poolId = poolId });
        }

        [HttpGet("Environments/{envId}/Pools/New")]
        public async Task<ActionResult<PoolConfigurationModel>> New(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Index", "Environments");
            }

            var result = new PoolConfigurationModel { EnvironmentName = envId };
            try
            {
                await PopulateSelectableProperties(environment, result);
            }
            catch (CloudException cEx)
            {
                // TODO: Log somewhere
                Console.WriteLine(cEx);
                ModelState.AddModelError("", $"Failed to configure pool creation form with error: {cEx.Message}");
            }

            return View(result);
        }

        [HttpPost("Environments/{envId}/Pools/New")]
        public async Task<ActionResult<PoolConfigurationModel>> New(string envId, PoolConfigurationModel model)
        {
            ValidateImageRefOrCustomAndSku(model);

            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Index", "Environments");
            }

            var packageErrors = false;
            InstallationPackage renderManagerPackage = null;
            var renderManagerPackageSelected = model.SelectedRenderManagerPackageId != NoPackageSelected;
            if (renderManagerPackageSelected)
            {
                renderManagerPackage = await _packageCoordinator.GetPackage(model.SelectedRenderManagerPackageId);
                if (renderManagerPackage == null)
                {
                    packageErrors = true;
                    ModelState.AddModelError(nameof(PoolConfigurationModel.SelectedRenderManagerPackageId), Validation.Errors.Custom.PackageNotFound);
                }
            }

            InstallationPackage gpuPackage = null;
            var gpuPackageSelected = model.SelectedGpuPackageId != NoPackageSelected;
            if (gpuPackageSelected)
            {
                gpuPackage = await _packageCoordinator.GetPackage(model.SelectedGpuPackageId);
                if (gpuPackage == null)
                {
                    packageErrors = true;
                    ModelState.AddModelError(nameof(PoolConfigurationModel.SelectedGpuPackageId), Validation.Errors.Custom.PackageNotFound);
                }
            }

            List<InstallationPackage> generalPackages = new List<InstallationPackage>();
            if (model.SelectedGeneralPackageIds != null)
            {
                foreach (var selectedGeneralPackageId in GetSelectedGeneralPackages(model.SelectedGeneralPackageIds))
                {
                    var package = await _packageCoordinator.GetPackage(selectedGeneralPackageId);
                    if (package == null)
                    {
                        packageErrors = true;
                        ModelState.AddModelError(nameof(PoolConfigurationModel.SelectedGeneralPackageIds), Validation.Errors.Custom.PackageNotFound);
                    }
                    else
                    {
                        generalPackages.Add(package);
                    }
                }
            }

            if (!ModelState.IsValid || packageErrors)
            {
                ModelState.AddModelError("", Validation.Errors.Custom.FormSummary);
                await PopulateSelectableProperties(environment, model);
                return View(model);
            }

            try
            {
                var mappedPool = await MapFromConfiguration(model, renderManagerPackage, gpuPackage, generalPackages);
                await _poolCoordinator.CreatePool(environment, mappedPool);
            }
            catch (CloudException cEx)
            {
                // TODO: Log somewhere
                Console.WriteLine(cEx);
                ModelState.AddModelError("", $"Pool creation failed with error: {cEx.Message}{cEx.Response?.Content}");
            }
            catch (Exception ex)
            {
                // TODO: Log somewhere
                Console.WriteLine(ex);
                ModelState.AddModelError("", $"Failed to configure pool with error: {ex}");
            }

            // if any of the above failed
            if (!ModelState.IsValid)
            {
                await PopulateSelectableProperties(environment, model);
                return View(model);
            }

            return RedirectToAction("Overview", new { envId, poolId = model.PoolName });
        }

        private void ValidateImageRefOrCustomAndSku(PoolConfigurationModel poolConfiguration)
        {
            if (string.IsNullOrWhiteSpace(poolConfiguration.ImageReference) && string.IsNullOrWhiteSpace(poolConfiguration.CustomImageReference))
            {
                ModelState.AddModelError(nameof(PoolConfigurationModel.ImageReference), Validation.Errors.Custom.ImageRefOrCustom);
                ModelState.AddModelError(nameof(PoolConfigurationModel.CustomImageReference), Validation.Errors.Custom.ImageRefOrCustom);
            }
        }

        private static IEnumerable<SelectListItem> MapOfficialImageReferencesToItems(IEnumerable<(string sku, PoolImageReference image)> inputs)
        {
            yield return new SelectListItem("Choose one...", "");

            var grouped = inputs.GroupBy(x => x.image.Publisher ?? "Custom");
            foreach (var group in grouped.OrderBy(g => g.Key))
            {
                var slig = new SelectListGroup { Name = group.Key };

                foreach (var x in group)
                {
                    var (sku, ir) = x;
                    if (ir.CustomImageResourceId != null)
                    {
                        throw new InvalidOperationException("Custom image passed as official image");
                    }

                    yield return new SelectListItem(ir.Name, ir.Value) { Group = slig };
                }
            }
        }

        private static IEnumerable<SelectListItem> MapCustomImageReferencesToItems(IEnumerable<PoolImageReference> inputs)
        {
            yield return new SelectListItem("Choose one...", "");

            foreach (var image in inputs)
            {
                if (image.CustomImageResourceId == null)
                {
                    throw new InvalidOperationException("Official image passed as custom image");
                }

                yield return new SelectListItem(image.Name, image.Value);
            }
        }

        private async Task PopulateSelectableProperties(RenderingEnvironment environment, PoolConfigurationModel result)
        {
            var (imageRefs, packages, sizes) = await (
                _poolCoordinator.GetImageReferences(environment),
                Task.WhenAll((await _packageCoordinator.ListPackages())
                    .Select(packageName => _packageCoordinator.GetPackage(packageName))),
                _vmSizes.GetSizesSelectList(environment));

            result.RenderManagerType = environment.RenderManager;
            result.RenderManagerPackages.Add(new SelectListItem("None", NoPackageSelected));

            switch (environment.RenderManager)
            {
                case RenderManagerType.Qube610:
                    result.RenderManagerPackages.AddRange(packages?
                        .Where(p => p.Type == InstallationPackageType.Qube610)
                        .Select(ip => new SelectListItem(ip.PackageName, ip.PackageName)).OrderBy(sli => sli.Text));
                    break;
                case RenderManagerType.Qube70:
                    result.RenderManagerPackages.AddRange(packages?
                        .Where(p => p.Type == InstallationPackageType.Qube70)
                        .Select(ip => new SelectListItem(ip.PackageName, ip.PackageName)).OrderBy(sli => sli.Text));
                    break;
                case RenderManagerType.Deadline:
                    result.RenderManagerPackages.AddRange(packages?
                        .Where(p => p.Type == InstallationPackageType.Deadline10)
                        .Select(ip => new SelectListItem(ip.PackageName, ip.PackageName)).OrderBy(sli => sli.Text));
                    break;
                case RenderManagerType.Tractor2:
                    result.RenderManagerPackages.AddRange(packages?
                        .Where(p => p.Type == InstallationPackageType.Tractor2)
                        .Select(ip => new SelectListItem(ip.PackageName, ip.PackageName)).OrderBy(sli => sli.Text));
                    break;
                case RenderManagerType.OpenCue:
                    result.RenderManagerPackages.AddRange(packages?
                        .Where(p => p.Type == InstallationPackageType.OpenCue)
                        .Select(ip => new SelectListItem(ip.PackageName, ip.PackageName)).OrderBy(sli => sli.Text));
                    break;
            }

            result.GpuPackages.Add(new SelectListItem("None", NoPackageSelected));
            result.GpuPackages.AddRange(packages?
                .Where(p => p.Type == InstallationPackageType.Gpu)
                .Select(ip => new SelectListItem(ip.PackageName, ip.PackageName)).OrderBy(sli => sli.Text));

            var generalPackages = packages?.Where(p => p.Type == InstallationPackageType.General).ToList();
            result.GeneralPackages.Add(new SelectListItem("None", NoPackageSelected, true));
            if (generalPackages != null && generalPackages.Any())
            {
                result.GeneralPackages.AddRange(
                    generalPackages
                        .Select(ip => new SelectListItem(ip.PackageName, ip.PackageName))
                        .OrderBy(sli => sli.Text));
            }

            result.OfficialImageReferences.AddRange(MapOfficialImageReferencesToItems(imageRefs.OfficialImages));
            result.CustomImageReferences.AddRange(MapCustomImageReferencesToItems(imageRefs.CustomImages));
            result.AutoScalePolicyItems.AddRange(
                Enum.GetValues(typeof(AutoScalePolicy)).Cast<AutoScalePolicy>()
                    .Select(p => new SelectListItem(p.GetDescription(), p.ToString())));
            result.AutoScalePolicyItems.First().Selected = true;
            result.Sizes.AddRange(sizes);
        }

        public async Task<Pool> MapFromConfiguration(
            PoolConfigurationModel poolConfiguration,
            InstallationPackage renderManagerPackage,
            InstallationPackage gpuPackage,
            List<InstallationPackage> generalPackages)
        {
            var environment = await _environmentCoordinator.GetEnvironment(poolConfiguration.EnvironmentName);
            if (environment == null)
            {
                throw new Exception($"Environment not found with name: {poolConfiguration.EnvironmentName}");
            }

            var metadata = new List<MetadataItem>();

            if (poolConfiguration.SelectedRenderManagerPackageId != NoPackageSelected)
            {
                metadata.Add(new MetadataItem(MetadataKeys.Package, poolConfiguration.SelectedRenderManagerPackageId));
            }

            if (poolConfiguration.SelectedGpuPackageId != NoPackageSelected)
            {
                metadata.Add(new MetadataItem(MetadataKeys.GpuPackage, poolConfiguration.SelectedGpuPackageId));
            }

            if (poolConfiguration.SelectedGeneralPackageIds != null)
            {
                metadata.Add(new MetadataItem(MetadataKeys.GeneralPackages, string.Join(',', poolConfiguration.SelectedGeneralPackageIds)));
            }

            metadata.Add(new MetadataItem(MetadataKeys.UseDeadlineGroups, poolConfiguration.UseDeadlineGroups.ToString()));

            metadata.AddAutoScaleMetadata(poolConfiguration);
            metadata.AddDeadlinePoolsAndGroups(poolConfiguration);

            if (!string.IsNullOrWhiteSpace(poolConfiguration.ExcludeFromLimitGroups))
            {
                metadata.Add(new MetadataItem(MetadataKeys.ExcludeFromLimitGroups, 
                    poolConfiguration.GetDeadlineExcludeFromLimitGroupsString(environment.RenderManagerConfig.Deadline)));
            }

            List<CertificateReference> certificates = null;
            if (!string.IsNullOrEmpty(environment.KeyVaultServicePrincipal.Thumbprint))
            {
                certificates = new List<CertificateReference>();
                certificates.Add(new CertificateReference(
                    environment.BatchAccount.GetCertificateId(environment.KeyVaultServicePrincipal.Thumbprint)));
            }

            var scaleSettings = new ScaleSettings(new FixedScaleSettings(
                targetDedicatedNodes: poolConfiguration.DedicatedNodes,
                targetLowPriorityNodes: poolConfiguration.LowPriorityNodes));

            PoolImageReference imageReference = GetPoolImageReference(poolConfiguration);
            var operatingSystem = imageReference.Os;

            var deploymentConfiguration = new DeploymentConfiguration
            {
                VirtualMachineConfiguration =
                    new VirtualMachineConfiguration
                    {
                        ImageReference = ToBatchImageReference(imageReference),
                        NodeAgentSkuId = imageReference.NodeAgentSku,
                    }
            };

            var appLicenses = new List<string>();
            if (poolConfiguration.MaxAppLicense)
            {
                appLicenses.Add("3dsmax");
            }

            if (poolConfiguration.MayaAppLicense)
            {
                appLicenses.Add("maya");
            }

            if (poolConfiguration.VrayAppLicense)
            {
                appLicenses.Add("vray");
            }

            if (poolConfiguration.ArnoldAppLicense)
            {
                appLicenses.Add("arnold");
            }

            NetworkConfiguration networkConfiguration = null;
            if (environment.Subnet?.ResourceId != null)
            {
                if (environment.LocationName.Equals(environment.Subnet?.Location, StringComparison.OrdinalIgnoreCase) &&
                    environment.BatchAccount.Location.Equals(environment.Subnet?.Location, StringComparison.OrdinalIgnoreCase))
                {
                    networkConfiguration = new NetworkConfiguration
                    {
                        SubnetId = environment.Subnet.ResourceId,
                    };
                }
                else
                {
                    Console.WriteLine("Subnet, Environment, and Batch account locations do not match. Subnet: {0}, Environment: {1}, Batch account: {2}",
                        environment.Subnet.Location,
                        environment.LocationName,
                        environment.BatchAccount.Location);
                }
            }

            var isWindows = imageReference.Os == Models.Pools.OperatingSystem.Windows;

            StartTask startTask = await _startTaskProvider.GetStartTask(
                        poolConfiguration,
                        environment,
                        renderManagerPackage,
                        gpuPackage,
                        generalPackages,
                        isWindows);

            var newPool = new Pool(
                name: poolConfiguration.PoolName,
                deploymentConfiguration: deploymentConfiguration,
                vmSize: poolConfiguration.VmSize,
                scaleSettings: scaleSettings,
                startTask: startTask,
                metadata: metadata,
                certificates: certificates,
                applicationLicenses: appLicenses);

            if (networkConfiguration != null)
            {
                newPool.NetworkConfiguration = networkConfiguration;
            }

            if (startTask != null)
            {
                newPool.StartTask = startTask;
            }

            return newPool;
        }

        private List<string> GetSelectedGeneralPackages(IEnumerable<string> selectedGeneralPackageIds)
        {
            var selectedPackages = new List<string>();
            if (selectedGeneralPackageIds != null)
            {
                selectedPackages.AddRange(selectedGeneralPackageIds.Where(p => p != NoPackageSelected));
            }
            return selectedPackages;
        }

        private PoolImageReference GetPoolImageReference(PoolConfigurationModel poolConfiguration)
        {
            if (!string.IsNullOrWhiteSpace(poolConfiguration.ImageReference))
            {
                return new PoolImageReference(poolConfiguration.ImageReference);
            }
            return new PoolImageReference(poolConfiguration.CustomImageReference);
        }

        private ImageReference ToBatchImageReference(PoolImageReference imageReference)
        {
            return new ImageReference(
                imageReference.Publisher, 
                imageReference.Offer, 
                imageReference.Sku, 
                imageReference.Version, 
                imageReference.CustomImageResourceId);
        }
    }
}

