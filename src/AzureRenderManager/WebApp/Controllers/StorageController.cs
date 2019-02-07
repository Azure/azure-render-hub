// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest.Azure;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using WebApp.Arm;
using WebApp.BackgroundHosts.Deployment;
using WebApp.Code.Attributes;
using WebApp.Code.Contract;
using WebApp.Config;
using WebApp.Config.Storage;
using WebApp.Identity;
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
        private readonly IHostingEnvironment _environment;
        private readonly IDeploymentQueue _deploymentQueue;
        private readonly IAzureResourceProvider _azureResourceProvider;
        private readonly IIdentityProvider _identityProvider;
        private readonly IManagementClientProvider _managementClientProvider;

        public StorageController(
            IHostingEnvironment environment,
            IConfiguration configuration,
            IDeploymentQueue deploymentQueue,
            CloudBlobClient cloudBlobClient,
            IAssetRepoCoordinator assetRepoCoordinator,
            IEnvironmentCoordinator environmentCoordinator,
            IPackageCoordinator packageCoordinator,
            IIdentityProvider identityProvider,
            IAzureResourceProvider azureResourceProvider,
            IManagementClientProvider managementClientProvider) : base(environmentCoordinator, packageCoordinator, assetRepoCoordinator)
        {
            _environment = environment;
            _configuration = configuration;
            _deploymentQueue = deploymentQueue;
            _identityProvider = identityProvider;
            _azureResourceProvider = azureResourceProvider;
            _managementClientProvider = managementClientProvider;
        }

        [HttpGet]
        [Route("Storage")]
        public ActionResult Index()
        {
            return View(new StorageConfigHomeModel());
        }

        [HttpDelete]
        [Route("Storage/Delete/{repoId}")]
        public async Task<ActionResult> Delete(string repoId)
        {
            var repository = await _assetRepoCoordinator.GetRepository(repoId);
            if (repository == null)
            {
                return NotFound($"Storage configuration with id: '{repoId}' could not be found");
            }

            // TODO: Remove this when christian is happy to delete it
            if (repository.Name.Equals("NfsServer", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict("Safeguard to stop me accidentally deleting this one");
            }

            repository.ProvisioningState = "Deleting";
            await _assetRepoCoordinator.UpdateRepository(repository);

            using (var networkClient = await _managementClientProvider.CreateNetworkManagementClient(repository.SubscriptionId))
            using (var computeClient = await _managementClientProvider.CreateComputeManagementClient(repository.SubscriptionId))
            {
                switch (repository)
                {
                    case AvereCluster avere:
                        throw new NotSupportedException("Unknown type of repository");

                    case NfsFileServer nfs:
                        await DeleteNfsFileServerDeployment(networkClient, computeClient, nfs);
                        break;

                    default:
                        throw new NotSupportedException("Unknown type of repository");
                }
            }

            var removed = await _assetRepoCoordinator.RemoveRepository(repository);
            if (!removed)
            {
                return StatusCode(500, "Unable to remove environment");
            }

            return Ok();
        }

        [HttpGet]
        [Route("Storage/{repoId}/Details")]
        public async Task<ActionResult> Details(string repoId)
        {
            var repo = await _assetRepoCoordinator.GetRepository(repoId);
            if (repo == null)
            {
                return RedirectToAction("Step1", new { repoId });
            }

            var model = await GetDetailsViewModel(repo);
            return View("View/Details", model);
        }

        [HttpGet]
        [Route("Storage/{repoId}/Configuration")]
        public async Task<ActionResult> Configuration(string repoId)
        {
            var repo = await _assetRepoCoordinator.GetRepository(repoId);
            if (repo == null)
            {
                return RedirectToAction("Step1", new { repoId });
            }

            var model = await GetDetailsViewModel(repo);
            return View("View/Configuration", model);
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
                        return RedirectToAction("Details", new { repoId });
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

            return View("Create/Step1", model);
        }

        [HttpPost]
        public async Task<ActionResult> Step1(AddAssetRepoStep1Model model)
        {
            if (!ModelState.IsValid)
            {
                // Validation errors, redirect back to form
                return View("Create/Step1", model);
            }

            // always get the repo with the originally set name
            var repository = await _assetRepoCoordinator.GetRepository(model.OriginalName ?? model.RepositoryName)
                ?? _assetRepoCoordinator.CreateRepository(model);

            if (repository.Enabled)
            {
                // not allowed to edit an existing enabled config
                return RedirectToAction("Details", new { repoId = repository.Name });
            }

            try
            {
                repository.Name = model.RepositoryName;
                repository.ResourceGroupName = model.RepositoryName;
                repository.RepositoryType = model.RepositoryType;
                repository.SubscriptionId = model.SubscriptionId.Value;
                repository.Subnet = new Subnet
                {
                    ResourceId = model.SubnetResourceIdLocationAndAddressPrefix.Split(";")[0],
                    Location = model.SubnetResourceIdLocationAndAddressPrefix.Split(";")[1],
                    AddressPrefix = model.SubnetResourceIdLocationAndAddressPrefix.Split(";")[2],
                };

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

        [HttpGet]
        [Route("Storage/Step2/{repoId?}")]
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
                return RedirectToAction("Details", new { repoId });
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
                case AvereCluster avere:
                    return View("Create/Step2Avere", new AddAvereClusterModel(avere)
                    {
                        Error = error,
                        ErrorMessage = errorMessage
                    });

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

                default:
                    throw new NotSupportedException("Unknown type of repository");
            }
        }

        [HttpPost]
        public async Task<ActionResult> Step2Nfs(AddNfsFileServerModel model)
        {
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
                return RedirectToAction("Details", new { repoId = repository.Name });
            }

            // validate the resource group doesn't exist
            using (var client = await GetResourceClient(model.SubscriptionId.ToString()))
            {
                try
                {
                    await client.ResourceGroups.CreateOrUpdateAsync(model.NewResourceGroupName,
                        new ResourceGroup(repository.Subnet.Location));

                    await _azureResourceProvider.AssignManagementIdentityAsync(
                        repository.SubscriptionId,
                        repository.ResourceGroupResourceId,
                        AzureResourceProvider.ContributorRole,
                        _identityProvider.GetPortalManagedServiceIdentity());

                    // update and save the model before we deploy as we can always retry the create
                    repository.UpdateFromModel(model);
                    repository.ProvisioningState = "Creating";
                    repository.DeploymentName = "file-server-deploy";
                    repository.InProgress = false;

                    await _assetRepoCoordinator.UpdateRepository(repository);

                    await DeployNfsFileServer(client, repository as NfsFileServer, model.Password);
                }
                catch (Exception ex)
                {
                    model.Error = "Failed to create repository with error";
                    model.ErrorMessage = ex.ToString();
                    return View("Create/Step2Nfs", model);
                }
            }

            return RedirectToAction("Details", new { repoId = repository.Name });
        }

        [HttpPost]
        public async Task<ActionResult> Step2Avere(AddAvereClusterModel model)
        {
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
                return RedirectToAction("Details", new { repoId = repository.Name });
            }

            // validate the resource group doesn't exist
            var client = await GetResourceClient(model.SubscriptionId.ToString());
            if (!await ValidateResourceGroup(client, model.NewResourceGroupName))
            {
                return View("Create/Step2Avere", model);
            }

            try
            {
                // update and save the model before we deploy as we can always retry the create
                repository.UpdateFromModel(model);
                repository.ProvisioningState = "Creating";
                repository.DeploymentName = "avere-deploy";
                repository.InProgress = false;

                await _assetRepoCoordinator.UpdateRepository(repository);

                await DeployAvereCluster(client, repository as AvereCluster);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Failed to create repository with error: {ex}");
                return View("Create/Step2Avere", model);
            }

            return RedirectToAction("Details", new { repoId = repository.Name });
        }

        [HttpPost]
        public async Task<ActionResult> Shutdown(string fileServerName)
        {
            var fileServer = await _assetRepoCoordinator.GetRepository(fileServerName) as NfsFileServer;
            if (fileServer == null)
            {
                return NotFound($"No NFS File Server found with the name: {fileServerName}");
            }

            using (var computeClient = await _managementClientProvider.CreateComputeManagementClient(fileServer.SubscriptionId))
            {
                await computeClient.VirtualMachines.BeginDeallocateAsync(fileServer.ResourceGroupName, fileServer.VmName);
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult> Start(string fileServerName)
        {
            var fileServer = await _assetRepoCoordinator.GetRepository(fileServerName) as NfsFileServer;
            if (fileServer == null)
            {
                return NotFound($"No NFS File Server found with the name: {fileServerName}");
            }

            using (var computeClient = await _managementClientProvider.CreateComputeManagementClient(fileServer.SubscriptionId))
            {
                await computeClient.VirtualMachines.BeginStartWithHttpMessagesAsync(fileServer.ResourceGroupName, fileServer.VmName);
            }

            return NoContent();
        }

        // #########################################################
        // TODO: below here are other methods that can be injectable
        // #########################################################

        private async Task<string> GetVirtualMachineStatus(Guid subscriptionId, string rgName, string vmName)
        {
            var status = "Unknown";
            if (string.IsNullOrEmpty(rgName) || string.IsNullOrEmpty(vmName))
            {
                return status;
            }

            using (var computeClient = await _managementClientProvider.CreateComputeManagementClient(subscriptionId))
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

        private async Task DeployAvereCluster(ResourceManagementClient client, AvereCluster repository)
        {
            if (repository == null)
            {
                throw new ArgumentException("DeployAvereCluster was passed a null repository");
            }

            await Task.CompletedTask;
        }

        private async Task<bool> DeployNfsFileServer(ResourceManagementClient client, NfsFileServer repository, string password)
        {
            if (repository == null)
            {
                throw new ArgumentException("DeployNfsFileServer was passed a null repository");
            }

            try
            {
                await client.ResourceGroups.CreateOrUpdateAsync(repository.ResourceGroupName,
                    new ResourceGroup {Location = repository.Subnet.Location});

                var fileShare = repository.FileShares.FirstOrDefault();
                var templateParams = new Dictionary<string, Dictionary<string, object>>
                {
                    {"vmName", new Dictionary<string, object> {{"value", repository.VmName}}},
                    {"adminUserName", new Dictionary<string, object> {{"value", repository.Username}}},
                    {"adminPassword", new Dictionary<string, object> {{"value", password}}},
                    {"vmSize", new Dictionary<string, object> {{"value", repository.VmSize}}},
                    {"subnetResourceId", new Dictionary<string, object> {{"value", repository.Subnet.ResourceId}}},
                    {"sharesToExport", new Dictionary<string, object> {{"value", fileShare?.Name ?? ""}}},
                };

                var file = new FileInfo(Path.Combine(_environment.ContentRootPath, "Templates", "linux-file-server.json"));
                var properties = new Deployment
                {
                    Properties = new DeploymentProperties
                    {
                        Template = JObject.Parse(await System.IO.File.ReadAllTextAsync(file.FullName)),
                        Parameters = templateParams,
                        Mode = DeploymentMode.Incremental
                    }
                };

                await client.Deployments.BeginCreateOrUpdateAsync(
                    repository.ResourceGroupName,
                    repository.DeploymentName,
                    properties);

                await _deploymentQueue.Add(new ActiveDeployment
                {
                    FileServerName = repository.Name,
                    StartTime = DateTime.UtcNow,
                });

                // TODO: can the deployment queue update the state to running when it's done??
                repository.ProvisioningState = "Deploying";
                await _assetRepoCoordinator.UpdateRepository(repository);
            }
            catch (CloudException ex)
            {
                Console.WriteLine($"Failed to deploy NFS server: {ex.Message}.\n{ex.StackTrace}");
                throw;
            }

            return true;
        }

        private async Task<bool> DeleteNfsFileServerDeployment(INetworkManagementClient networkClient
            , IComputeManagementClient computeClient, NfsFileServer repository)
        {
            try
            {
                var virtualMachine = await computeClient.VirtualMachines.GetAsync(repository.ResourceGroupName, repository.VmName);
                if (virtualMachine != null)
                {
                    var nicId = virtualMachine.NetworkProfile.NetworkInterfaces[0].Id;
                    var avSet = virtualMachine.AvailabilitySet.Id?.Split("/").Last();
                    var osDisk = virtualMachine.StorageProfile.OsDisk.ManagedDisk.Id;
                    var dataDisks = virtualMachine.StorageProfile.DataDisks.Select(dd => dd.ManagedDisk.Id.Split("/").Last()).ToList();

                    var nic = await networkClient.NetworkInterfaces.GetAsync(repository.ResourceGroupName, nicId.Split("/").Last());
                    var pip = nic.IpConfigurations[0].PublicIPAddress?.Id;
                    var nsg = nic.NetworkSecurityGroup?.Id;

                    await _deploymentQueue.Add(new ActiveDeployment
                    {
                        FileServerName = repository.Name,
                        StartTime = DateTime.UtcNow,
                        Action = "DeleteVM",
                        AvSetName = avSet,
                        NicName = nic.Name,
                        NsgName = nsg?.Split("/").Last(),
                        PipName = pip?.Split("/").Last(),
                        OsDiskName = osDisk?.Split("/").Last(),
                        DataDiskNames = dataDisks,
                    });

                    return true;
                }
                else
                {
                    // TODO: Log and return something ...
                    Console.WriteLine($"No virtual machine found with name: {repository.VmName}");
                }
            }
            catch (Exception ex)
            {
                // TODO: Log Exception
                // TODO: Do we care? Should we return a message to the user saying do it yourself?
                Console.WriteLine($"Failed to delete NFS server: {ex.Message}.\n{ex.StackTrace}");
            }

            return false;
        }

        // #########################################################
        // TODO: General Helper Methods for the Controller
        // #########################################################

        private async Task<AssetRepositoryDetailsModel> GetDetailsViewModel(AssetRepository repo)
        {
            switch (repo)
            {
                case AvereCluster avere:
                    return new AvereClusterDetailsModel(avere) { Status = repo.ProvisioningState };

                case NfsFileServer nfs:
                    var status = await GetVirtualMachineStatus(nfs.SubscriptionId, nfs.ResourceGroupName, nfs.VmName);
                    return new NfsFileServerDetailsModel(nfs)
                    {
                        Status = status == "Unknown" ? repo.ProvisioningState ?? "Unknown" : status
                    };

                default:
                    throw new NotSupportedException("Unknown type of repository");
            }
        }
    }
}
