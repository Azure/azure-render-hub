// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AzureRenderHub.WebApp.Arm.Deploying;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web.Client;
using Microsoft.Rest.Azure;
using WebApp.Arm;
using WebApp.Code.Attributes;
using WebApp.Code.Contract;
using WebApp.Config;
using WebApp.Config.Resources;
using WebApp.Config.Storage;
using WebApp.Models.Storage;
using WebApp.Models.Storage.Create;
using WebApp.Models.Storage.Details;
using WebApp.Operations;

namespace WebApp.Controllers
{
    [MenuActionFilter]
    [StorageActionFilter]
    public class StorageController : MenuBaseController
    {
        private readonly IConfiguration _configuration;
        private readonly IManagementClientProvider _managementClientProvider;
        private readonly IAzureResourceProvider _azureResourceProvider;
        private readonly IDeploymentCoordinator _deploymentCoordinator;
        private readonly ILogger _logger;

        public StorageController(
            IConfiguration configuration,
            IManagementClientProvider managementClientProvider,
            IAssetRepoCoordinator assetRepoCoordinator,
            IEnvironmentCoordinator environmentCoordinator,
            IPackageCoordinator packageCoordinator,
            IAzureResourceProvider azureResourceProvider,
            IDeploymentCoordinator deploymentCoordinator,
            ITokenAcquisition tokenAcquisition,
            ILogger<StorageController> logger) : base(
                environmentCoordinator,
                packageCoordinator, 
                assetRepoCoordinator,
                tokenAcquisition)
        {
            _configuration = configuration;
            _azureResourceProvider = azureResourceProvider;
            _managementClientProvider = managementClientProvider;
            _deploymentCoordinator = deploymentCoordinator;
            _logger = logger;
        }

        [HttpGet]
        [Route("Storage")]
        public ActionResult Index()
        {
            return View(new StorageConfigHomeModel());
        }

        [HttpGet]
        [Route("Storage/{repoId}/Delete")]
        public async Task<ActionResult> Delete(string repoId)
        {
            var repository = await _assetRepoCoordinator.GetRepository(repoId);
            if (repository == null)
            {
                return RedirectToAction("Index");
            }

            var model = new DeleteStorageModel(repository);
            
            try
            {
                var sudId = repository.SubscriptionId.ToString();
                var mapped = new List<GenericResource>();
                if (!string.IsNullOrEmpty(repository.ResourceGroupName))
                {
                    var resources = await _azureResourceProvider.ListResourceGroupResources(sudId, repository.ResourceGroupName);
                    mapped.AddRange(resources.Select(resource => new GenericResource(resource)));
                }

                model.Resources.AddRange(mapped);
            }
            catch (CloudException cEx)
            {
                if (cEx.Body?.Code != "ResourceGroupNotFound")
                {
                    model.ResourceLoadFailed = true;
                }
                ModelState.AddModelError("", $"Failed to list resources from the Resource Group with error: {cEx.Message}");
            }

            return View("View/Delete", model);
        }

        [HttpPost]
        [Route("Storage/{repoId}/Delete")]
        public async Task<ActionResult> Delete(string repoId, DeleteStorageModel model)
        {
            if (!model.Name.Equals(model.Confirmation, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(DeleteStorageModel.Confirmation),
                    $"The entered name must match '{model.Name}'");
            }

            if (!model.SubscriptionId.HasValue)
            {
                ModelState.AddModelError("", "Storage does not have a configured subscription ID. Deletion cannot continue.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var repository = await _assetRepoCoordinator.GetRepository(repoId);
            if (repository == null)
            {
                return RedirectToAction("Index");
            }

            await _assetRepoCoordinator.BeginDeleteRepositoryAsync(repository, model.DeleteResourceGroup);

            return RedirectToAction("Deploying", new { repoId });
        }

        [HttpGet]
        [Route("Storage/{repoId}/Deploying")]
        public async Task<ActionResult> Deploying(string repoId)
        {
            var repo = await _assetRepoCoordinator.GetRepository(repoId);
            if (repo == null)
            {
                return RedirectToAction("Index");
            }

            if (repo.State == AzureRenderHub.WebApp.Config.Storage.StorageState.Unknown && repo.Deployment == null)
            {
                // We have a legacy file server, we need to update the state
                repo.State = AzureRenderHub.WebApp.Config.Storage.StorageState.Ready;
                await _assetRepoCoordinator.UpdateRepository(repo);
                return RedirectToAction("Overview", new { repoId });
            }

            if (repo.State != AzureRenderHub.WebApp.Config.Storage.StorageState.Deleting)
            {
                await _assetRepoCoordinator.UpdateRepositoryFromDeploymentAsync(repo);

                if (repo.State == AzureRenderHub.WebApp.Config.Storage.StorageState.Ready)
                {
                    return RedirectToAction("Overview", new { repoId });
                }
            }

            var model = await GetOverviewViewModel(repo);

            return View("View/Deploying", model);
        }

        [HttpGet]
        [Route("Storage/{repoId}/Overview")]
        public async Task<ActionResult> Overview(string repoId)
        {
            var repo = await _assetRepoCoordinator.GetRepository(repoId);
            if (repo == null)
            {
                return RedirectToAction("Index");
            }

            if (repo.State == AzureRenderHub.WebApp.Config.Storage.StorageState.Unknown
                || repo.State == AzureRenderHub.WebApp.Config.Storage.StorageState.Creating
                || repo.State == AzureRenderHub.WebApp.Config.Storage.StorageState.Failed
                || repo.State == AzureRenderHub.WebApp.Config.Storage.StorageState.Deleting)
            {
                return RedirectToAction("Deploying", new { repoId });
            }

            if (repo is NfsFileServer nfs) {
                var model = new NfsFileServerOverviewModel(nfs)
                {
                    PowerStatus = await GetVirtualMachineStatus(nfs.SubscriptionId.ToString(), nfs.ResourceGroupName, nfs.VmName),
                };
                return View("View/OverviewFileServer", model);
            }
            else if (repo is AvereCluster avere)
            {
                var model = new AvereClusterOverviewModel(avere);
                // TODO: Get Avere status of each cluster node
                return View("View/OverviewAvere", model);
            }
            else
            {
                throw new NotSupportedException("Unknown type of repository");
            }
        }

        [HttpGet]
        [Route("Storage/{repoId}/Resources")]
        public async Task<ActionResult> Resources(string repoId)
        {
            var repo = await _assetRepoCoordinator.GetRepository(repoId);
            if (repo == null)
            {
                return RedirectToAction("Step1", new { repoId });
            }

            var model = await GetOverviewViewModel(repo);
            return View("View/Resources", model);
        }

        [HttpGet]
        [Route("Storage/New")]
        [Route("Storage/Step1/{repoId?}")]
        public async Task<ActionResult> Step1(string repoId)
        {
            var model = new AddAssetRepoStep1Model();
            if (!string.IsNullOrEmpty(repoId))
            {
                var existing = await _assetRepoCoordinator.GetRepository(repoId);
                if (existing != null)
                {
                    if (existing.Enabled)
                    {
                        // not allowed to edit an existing enabled config
                        return RedirectToAction("Overview", new { repoId });
                    }

                    model = new AddAssetRepoStep1Model(existing);
                }
                else
                {
                    model.RepositoryName = repoId;
                }
            }

            if (!model.SubscriptionId.HasValue)
            {
                model.SubscriptionId = Guid.Parse(_configuration["SubscriptionId"]);
            }

            model.Environments = await _environmentCoordinator.ListEnvironments();

            return View("Create/Step1", model);
        }

        [HttpPost]
        public async Task<ActionResult> Step1(AddAssetRepoStep1Model model)
        {
            RenderingEnvironment environment = null;
            if (model.UseEnvironment)
            {
                if (string.IsNullOrEmpty(model.SelectedEnvironmentName))
                {
                    ModelState.AddModelError(nameof(AddAssetRepoStep1Model.SelectedEnvironmentName), "Selected environment cannot be empty when using a environment.");
                }
                else
                {
                    environment = await _environmentCoordinator.GetEnvironment(model.SelectedEnvironmentName);
                    if (environment == null)
                    {
                        ModelState.AddModelError(nameof(AddAssetRepoStep1Model.SelectedEnvironmentName), "Selected environment doesn't exist.");
                    }
                }
            }

            if (!model.UseEnvironment)
            {
                if (string.IsNullOrWhiteSpace(model.SubnetResourceIdLocationAndAddressPrefix))
                {
                    ModelState.AddModelError(nameof(AddAssetRepoStep1Model.SubnetResourceIdLocationAndAddressPrefix), "Subnet resource cannot be empty when using a VNet.");
                }

                if (model.SubscriptionId == null)
                {
                    ModelState.AddModelError(nameof(AddAssetRepoStep1Model.SubscriptionId), "Subscription must be selected when not using an environment.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.Environments = await _environmentCoordinator.ListEnvironments();

                // Validation errors, redirect back to form
                return View("Create/Step1", model);
            }

            // always get the repo with the originally set name
            var repository = await _assetRepoCoordinator.GetRepository(model.OriginalName ?? model.RepositoryName)
                ?? _assetRepoCoordinator.CreateRepository(model);

            if (repository.Enabled)
            {
                // not allowed to edit an existing enabled config
                return RedirectToAction("Overview", new { repoId = repository.Name });
            }

            try
            {
                repository.State = AzureRenderHub.WebApp.Config.Storage.StorageState.Creating;
                repository.InProgress = true;
                repository.Name = model.RepositoryName;
                repository.ResourceGroupName = model.RepositoryName;
                repository.RepositoryType = model.RepositoryType;
                repository.EnvironmentName = null;
                
                if (model.UseEnvironment)
                {
                    repository.SubscriptionId = environment.SubscriptionId;
                    repository.EnvironmentName = model.SelectedEnvironmentName;
                    repository.Subnet = await GetAndVerifySubnet(environment.Subnet);
                }
                else
                {
                    repository.SubscriptionId = model.SubscriptionId.Value;
                    repository.Subnet = await GetAndVerifySubnet(model.Subnet);
                }

                // pass in the original name in case we have updated it.
                await _assetRepoCoordinator.UpdateRepository(repository, model.OriginalName);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Failed to create repository with error: {ex}");
                return View("Create/Step1", model);
            }

            return RedirectToAction("Step2", new { repoId = repository.Name });
        }

        private async Task<Subnet> GetAndVerifySubnet(Subnet specifiedSubnet)
        {
            var subnet = await _azureResourceProvider.GetSubnetAsync(
                specifiedSubnet.SubscriptionId,
                specifiedSubnet.Location,
                specifiedSubnet.ResourceGroupName,
                specifiedSubnet.VNetName,
                specifiedSubnet.Name);

            if (subnet == null)
            {
                throw new Exception($"Subnet with resource Id {specifiedSubnet.ResourceId} " +
                    $"was not found in Subscription {specifiedSubnet.SubscriptionId}");
            }

            return subnet;
        }

        [HttpGet]
        [Route("Storage/{repoId}/Step2")]
        public async Task<ActionResult> Step2(string repoId)
        {
            var repository = await _assetRepoCoordinator.GetRepository(repoId);
            if (repository == null)
            {
                // redirect to Step1 if no config.
                return RedirectToAction("Step1", new { repoId });
            }

            if (repository.Enabled)
            {
                // not allowed to edit an existing enabled config
                return RedirectToAction("Overview", new { repoId });
            }

            string error = null;
            string errorMessage = null;

            var canCreate = await _azureResourceProvider.CanCreateResources(repository.SubscriptionId);
            if (!canCreate)
            {
                error = "You don't have the required permissions to create resources";
                errorMessage = "In order to complete this step which involves creating resources, you must have the Owner or Contributor role for the specified Subscription. " +
                                     "Either request someone with this role to complete the step, or ask your admin to make you an Owner or Contributor for the Subscription.";
            }
            else
            {
                var canAssign = await _azureResourceProvider.CanCreateRoleAssignments(
                    repository.SubscriptionId,
                    repository.ResourceGroupName);
                if (!canAssign)
                {
                    error = "You don't have the required permissions to assign roles to users";
                    errorMessage = "In order to complete this step which involves creating role assignments, you must have the Owner or User Access Administrator role for the specified Subscription. " +
                                   "Either request someone with this role to complete the step, or ask your admin to make you an Owner or User Access Administrator for the Subscription or Resource Group.";
                }
            }

            switch (repository)
            {
                case NfsFileServer nfs:
                    return View("Create/Step2Nfs", new AddNfsFileServerModel(nfs)
                    {
                        VmName = "FileServer",
                        UserName = "fileserver",
                        Password = Guid.NewGuid().ToString(),
                        FileShareName = "/exports/share",
                        Error = error,
                        ErrorMessage = errorMessage
                    });

                case AvereCluster avere:
                    var model = new AddAvereClusterModel(avere)
                    {
                        ExistingSubnets = await _azureResourceProvider.GetSubnetsAsync(
                            avere.Subnet.SubscriptionId,
                            avere.Subnet.Location,
                            avere.Subnet.ResourceGroupName,
                            avere.Subnet.VNetName),
                        Error = error,
                        ErrorMessage = errorMessage
                    };

                    return View("Create/Step2Avere", model);

                default:
                    throw new NotSupportedException("Unknown type of repository");
            }
        }

        [HttpPost]
        public async Task<ActionResult> Step2Nfs(AddNfsFileServerModel model)
        {
            // Validate that the share isn't a root share
            if (model.FileShareName.StartsWith("/") &&
                model.FileShareName.Split("/", StringSplitOptions.RemoveEmptyEntries).Length == 1)
            {
                ModelState.AddModelError(nameof(AddNfsFileServerModel.FileShareName), "File share cannot be at the root of the file system, e.g. /share.");
            }

            if (!ModelState.IsValid)
            {
                return View("Create/Step2Nfs", model);
            }

            var repository = await _assetRepoCoordinator.GetRepository(model.RepositoryName);
            if (repository == null)
            {
                return BadRequest("No new storage repository configuration in progress");
            }

            if (repository.Enabled)
            {
                // not allowed to edit an existing enabled config
                return RedirectToAction("Overview", new { repoId = repository.Name });
            }

            try
            {
                repository.UpdateFromModel(model);
                await _assetRepoCoordinator.BeginRepositoryDeploymentAsync(repository);
            }
            catch (Exception ex)
            {
                model.Error = "Failed to create repository with error";
                model.ErrorMessage = ex.ToString();
                return View("Create/Step2Nfs", model);
            }

            return RedirectToAction("Deploying", new { repoId = repository.Name });
        }

        [HttpPost]
        public async Task<ActionResult> Step2Avere(AddAvereClusterModel model)
        {
            if (model.UseControllerPasswordCredential && string.IsNullOrWhiteSpace(model.ControllerPassword))
            {
                ModelState.AddModelError(nameof(AddAvereClusterModel.ControllerPassword), 
                    "A controller password must be specified.");
            }

            if (!model.UseControllerPasswordCredential && string.IsNullOrWhiteSpace(model.ControllerSshKey))
            {
                ModelState.AddModelError(nameof(AddAvereClusterModel.ControllerSshKey),
                    "A controller SSH key must be specified.");
            }

            var allowedVMSizes = new [] { "standard_e8s_v3", "standard_e16s_v3", "standard_e32s_v3", "standard_d16s_v3" };
            if (!allowedVMSizes.Contains(model.VMSize.ToLowerInvariant()))
            {
                ModelState.AddModelError(nameof(AddAvereClusterModel.VMSize),
                    $"The Avere vFXT VM size must be one of {string.Join(", ", allowedVMSizes)}");
            }

            if (model.CacheSizeInGB != 1024 && model.CacheSizeInGB != 4096)
            {
                ModelState.AddModelError(nameof(AddAvereClusterModel.CacheSizeInGB),
                    $"The Avere vFXT cache size must be either 1024 or 4096.");
            }

            if (model.CreateSubnet)
            {
                if (string.IsNullOrWhiteSpace(model.NewSubnetName))
                {
                    ModelState.AddModelError(nameof(AddAvereClusterModel.NewSubnetName),
                        $"When Create Subnet is specified, a subnet name must be specified.");
                }

                if (string.IsNullOrWhiteSpace(model.NewSubnetAddressPrefix))
                {
                    ModelState.AddModelError(nameof(AddAvereClusterModel.NewSubnetAddressPrefix),
                        $"When Create Subnet is specified, a subnet address prefix (CIDR block) must be specified.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View("Create/Step2Avere", model);
            }

            var repository = await _assetRepoCoordinator.GetRepository(model.RepositoryName);
            if (repository == null)
            {
                return BadRequest("No new storage repository configuration in progress");
            }

            if (repository.Enabled)
            {
                // not allowed to edit an existing enabled config
                return RedirectToAction("Overview", new { repoId = repository.Name });
            }

            // validate the resource group doesn't exist
            var client = await _managementClientProvider.CreateResourceManagementClient(model.SubscriptionId.Value);
            if (!await ValidateResourceGroup(client, model.NewResourceGroupName, nameof(model.NewResourceGroupName)))
            {
                return View("Create/Step2Avere", model);
            }

            try
            {
                // update and save the model before we deploy as we can always retry the create
                repository.UpdateFromModel(model);

                if (model.CreateSubnet)
                {
                    // We create the subnet here, it's faster and we get an immediate failure
                    // if there's validation or permission issues.
                    repository.Subnet = await _azureResourceProvider.CreateSubnetAsync(
                        repository.Subnet.SubscriptionId,
                        repository.Subnet.Location,
                        repository.Subnet.ResourceGroupName,
                        repository.Subnet.VNetName,
                        repository.Subnet.Name,
                        repository.Subnet.AddressPrefix,
                        repository.EnvironmentName);

                    // Here we update the subnet we just created with 
                    // Storage service endpoints, required for Avere.
                    var deploymentSpec = new Deployment(
                        repository.SubscriptionId,
                        repository.Subnet.Location,
                        repository.Subnet.ResourceGroupName,
                        $"subnet-{Guid.NewGuid()}");

                    var subnetDeployment = new SubnetDeployment(
                        deploymentSpec,
                        repository.Subnet.VNetName,
                        repository.Subnet.Name,
                        repository.Subnet.AddressPrefix);

                    await _deploymentCoordinator.BeginDeploymentAsync(subnetDeployment);

                    var deploymentStatus = await _deploymentCoordinator.WaitForCompletionAsync(subnetDeployment);

                    if (deploymentStatus.ProvisioningState != ProvisioningState.Succeeded)
                    {
                        throw new Exception($"Subnet deployment {deploymentSpec.DeploymentName} failed with error {deploymentStatus.Error}");
                    }
                }

                await _assetRepoCoordinator.BeginRepositoryDeploymentAsync(repository);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create repository {repository.Name} in subscription {repository.SubscriptionId}");
                ModelState.AddModelError("", $"Failed to create repository: {ex.Message}");
                return View("Create/Step2Avere", model);
            }

            return RedirectToAction("Overview", new { repoId = repository.Name });
        }

        [HttpGet]
        [Route("Storage/{repoId}/PowerOperation/{operation}")]
        public async Task<ActionResult> PowerOperation(string repoId, string operation)
        {
            var fileServer = await _assetRepoCoordinator.GetRepository(repoId) as NfsFileServer;
            if (fileServer == null)
            {
                return NotFound($"No NFS File Server found with the name: {repoId}");
            }

            if (operation != "start" && operation != "shutdown")
            {
                return BadRequest("Action must be 'start' or 'shutdown'");
            }

            using (var computeClient = await _managementClientProvider.CreateComputeManagementClient(fileServer.SubscriptionId))
            {
                if (operation == "start")
                {
                    await computeClient.VirtualMachines.BeginStartWithHttpMessagesAsync(fileServer.ResourceGroupName, fileServer.VmName);
                }
                else if (operation == "shutdown")
                {
                    await computeClient.VirtualMachines.BeginDeallocateAsync(fileServer.ResourceGroupName, fileServer.VmName);
                }
            }

            return RedirectToAction("Overview", new { repoId = repoId });
        }

        private async Task<string> GetVirtualMachineStatus(string subscriptionId, string rgName, string vmName)
        {
            var status = "Unknown";
            if (string.IsNullOrEmpty(rgName) || string.IsNullOrEmpty(vmName))
            {
                return status;
            }

            using (var computeClient = await _managementClientProvider.CreateComputeManagementClient(Guid.Parse(subscriptionId)))
            {
                try
                {
                    var node = await computeClient.VirtualMachines.GetAsync(rgName, vmName, InstanceViewTypes.InstanceView);
                    status = node?.InstanceView?.Statuses?.FirstOrDefault(s => s.Code.StartsWith("PowerState/"))?.DisplayStatus;
                }
                catch (CloudException cEx)
                {
                    if (cEx.Response.StatusCode == HttpStatusCode.NotFound || cEx.Body.Code == "NotFound")
                    {
                        // Ignore
                        Console.WriteLine($"Failed to get VM status as VM: {vmName} was not found.");
                    }
                    else
                    {
                        // TODO: Log or do something else ...
                        Console.WriteLine($"Failed to get VM status with error: {cEx.Message}.\n{cEx.StackTrace}");
                    }
                }
            }

            return status;
        }

        private async Task<AssetRepositoryOverviewModel> GetOverviewViewModel(AssetRepository repo)
        {
            switch (repo)
            {
                case NfsFileServer nfs:
                    return new NfsFileServerOverviewModel(nfs)
                    {
                        PowerStatus = await GetVirtualMachineStatus(nfs.SubscriptionId.ToString(), nfs.ResourceGroupName, nfs.VmName),
                    };
                case AvereCluster avere:
                    return new AvereClusterOverviewModel(avere);
                default:
                    throw new NotSupportedException("Unknown type of repository");
            }
        }
    }
}
