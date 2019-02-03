// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Batch;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest.Azure;
using TaskTupleAwaiter;
using WebApp.AppInsights.PoolUsage;
using WebApp.Arm;
using WebApp.Code.Attributes;
using WebApp.Code.Contract;
using WebApp.Config;
using WebApp.Config.Pools;
using WebApp.Models.Environments.Create;
using WebApp.Models.Environments.Details;
using WebApp.Config.RenderManager;
using WebApp.Config.Resources;
using WebApp.Identity;
using WebApp.Models.Environments;
using WebApp.Operations;


namespace WebApp.Controllers
{
    [MenuActionFilter]
    [EnvironmentsActionFilter]
    public class EnvironmentsController : MenuBaseController, IEnvController
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureResourceProvider _azureResourceProvider;
        private readonly IKeyVaultMsiClient _keyVaultMsiClient;
        private readonly IIdentityProvider _identityProvider;
        private readonly IEnvironmentCoordinator _environmentCoordinator;
        private readonly IManagementClientProvider _managementClientProvider;
        private readonly IPoolUsageProvider _poolUsageProvider;
        private readonly StartTaskProvider _startTaskProvider;

        public EnvironmentsController(
            IConfiguration configuration,
            IAzureResourceProvider azureResourceProvider,
            IKeyVaultMsiClient keyVaultMsiClient,
            IIdentityProvider identityProvider,
            IEnvironmentCoordinator environmentCoordinator,
            IManagementClientProvider managementClientProvider,
            IPoolUsageProvider poolUsageProvider,
            IPackageCoordinator packageCoordinator,
            IAssetRepoCoordinator assetRepoCoordinator,
            StartTaskProvider startTaskProvider) : base(environmentCoordinator, packageCoordinator, assetRepoCoordinator)
        {
            _configuration = configuration;
            _azureResourceProvider = azureResourceProvider;
            _keyVaultMsiClient = keyVaultMsiClient;
            _identityProvider = identityProvider;
            _environmentCoordinator = environmentCoordinator;
            _managementClientProvider = managementClientProvider;
            _poolUsageProvider = poolUsageProvider;
            _startTaskProvider = startTaskProvider;
        }

        [HttpGet]
        [Route("Environments")]
        public async Task<ActionResult> Index()
        {
            var model = new EnvironmentOverviewModel();
            var tasks = new List<Task<ViewEnvironmentModel>>();
            var envs = await _environmentCoordinator.ListEnvironments();
            foreach (var env in envs)
            {
                tasks.Add(GetEnvironmentModel(env));
            }

            foreach (var task in tasks)
            {
                if (task != null)
                {
                    var result = await task;
                    if (result != null)
                    {
                        model.Environments.Add(result);
                    }
                }
            }
            return View(model);
        }

        private async Task<ViewEnvironmentModel> GetEnvironmentModel(string environmentName)
        {
            var environment = await _environmentCoordinator.GetEnvironment(environmentName);
            if (environment == null || !environment.Enabled)
            {
                return null;
            }

            var usage = await _poolUsageProvider.GetEnvironmentUsage(environment);

            return new ViewEnvironmentModel(environment, poolUsageResults: usage);
        }

        [HttpGet]
        [Route("Environments/Delete/{envId}")]
        public async Task<ActionResult> Delete(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return NotFound($"Environment with id: '{envId}' could not be found");
            }

            if (environment.State == EnvironmentState.Deleting)
            {
                // if we are already being deleted then go to the deletion overview
                return RedirectToAction("Deleting", new { envId = environment.Name });
            }

            var model = new DeleteEnvironmentModel(environment);
            try
            {
                var sudId = environment.SubscriptionId.ToString();
                var resources = await _azureResourceProvider.ListResourceGroupResources(sudId, environment.ResourceGroupName);
                var mapped = resources.Select(resource => new GenericResource(resource)).ToList();
                model.Resources.AddRange(mapped);

                return View(model);
            }
            catch (CloudException cEx)
            {
                model.ResourceLoadFailed = true;
                ModelState.AddModelError("", $"Failed to list resources from the Resource Group with error: {cEx.Message}");

                return View(model);
            }
        }

        /// <summary>
        /// TODO: Do we need to delete the Service Principal?
        /// az ad sp delete --id http://andrew-tractor-australiaeastMgmtSP
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Delete(DeleteEnvironmentModel model)
        {
            if (model.EnvironmentName.Equals(model.Confirmation, StringComparison.OrdinalIgnoreCase) == false)
            {
                ModelState.AddModelError(nameof(DeleteEnvironmentModel.Confirmation),
                    $"The entered name must match '{model.EnvironmentName}'");
            }

            if (!model.SubscriptionId.HasValue)
            {
                ModelState.AddModelError("",
                    "Environment does not have a configured subscription ID. Deletion cannot continue.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var environment = await _environmentCoordinator.GetEnvironment(model.EnvironmentName);
            if (environment == null)
            {
                return BadRequest(
                    $"No new environment configuration was found with the name: '{model.EnvironmentName}'");
            }

            environment.State = EnvironmentState.Deleting;
            environment.DeletionSettings = new DeletionSettings
            {
                DeleteResourceGroup = model.DeleteResourceGroup,
                DeleteBatchAccount = model.DeleteBatchAccount,
                DeleteStorageAccount = model.DeleteStorageAccount,
                DeleteAppInsights = model.DeleteAppInsights,
                DeleteKeyVault = model.DeleteKeyVault,
                DeleteVNet = model.DeleteVNet,
            };
            await _environmentCoordinator.UpdateEnvironment(environment);

            return RedirectToAction("Deleting", new { envId = environment.Name });
        }

        private async Task DeleteEnvironment(RenderingEnvironment environment)
        {
            var deletingTask = environment.DeletionSettings.DeleteResourceGroup
                ? DeleteResourceGroup(environment)
                : DeleteIndividualResources(environment);

            environment.State = EnvironmentState.Deleting;

            try
            {
                await deletingTask;
            }
            catch (Exception ex)
            {
                environment.State = EnvironmentState.DeleteFailed;
                environment.DeletionSettings.DeleteErrors = ex.ToString();
            }

            if (environment.State == EnvironmentState.Deleting)
            {
                await _environmentCoordinator.RemoveEnvironment(environment);
            }
            else
            {
                await _environmentCoordinator.UpdateEnvironment(environment);
            }
        }

        private async Task DeleteResourceGroup(RenderingEnvironment environment)
        {
            await _azureResourceProvider.DeleteResourceGroupAsync(environment.SubscriptionId, environment.ResourceGroupName);
        }

        private async Task DeleteIndividualResources(RenderingEnvironment environment)
        {
            var deleteMe = new List<Task>();
            if (environment.DeletionSettings.DeleteBatchAccount && environment.BatchAccount != null)
            {
                deleteMe.Add(
                    _azureResourceProvider.DeleteBatchAccountAsync(
                        environment.BatchAccount.SubscriptionId,
                        environment.BatchAccount.ResourceGroupName,
                        environment.BatchAccount.Name));
            }

            if (environment.DeletionSettings.DeleteStorageAccount && environment.StorageAccount != null)
            {
                deleteMe.Add(_azureResourceProvider.DeleteStorageAccountAsync(
                    environment.StorageAccount.SubscriptionId,
                    environment.StorageAccount.ResourceGroupName,
                    environment.StorageAccount.Name));
            }

            if (environment.DeletionSettings.DeleteAppInsights && environment.ApplicationInsightsAccount != null)
            {
                deleteMe.Add(_azureResourceProvider.DeleteApplicationInsightsAsync(
                    environment.ApplicationInsightsAccount.SubscriptionId,
                    environment.ApplicationInsightsAccount.ResourceGroupName,
                    environment.ApplicationInsightsAccount.Name));
            }

            if (environment.DeletionSettings.DeleteKeyVault && environment.KeyVault != null)
            {
                deleteMe.Add(_azureResourceProvider.DeleteKeyVaultAsync(
                    environment.KeyVault.SubscriptionId,
                    environment.KeyVault));
            }

            if (environment.DeletionSettings.DeleteVNet && environment.Subnet != null)
            {
                deleteMe.Add(_azureResourceProvider.DeleteVNetAsync(
                    environment.Subnet.SubscriptionId,
                    environment.Subnet.ResourceGroupName,
                    environment.Subnet.VNetName));
            }

            if (deleteMe.Any())
            {
                await Task.WhenAll(deleteMe);
            }
        }

        [HttpGet]
        [Route("Environments/{envId}/Deleting")]
        public async Task<IActionResult> Deleting(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Index");
            }

            if (environment.State != EnvironmentState.Deleting && environment.State != EnvironmentState.DeleteFailed)
            {
                return RedirectToAction("Index");
            }

            await DeleteEnvironment(environment);

            var model = new DeletingEnvironmentModel(environment);

            return View(model);
        }

        [HttpGet]
        [HttpDelete]
        [Route("Environments/{envId}/Remove")]
        public async Task<ActionResult> Remove(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return NotFound($"Environment with id: '{envId}' could not be found");
            }

            try
            {
                await _environmentCoordinator.RemoveEnvironment(environment);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to remove the environment  '{envId}': {ex}");
            }
        }

        [HttpGet]
        [Route("Environments/{envId}/Overview")]
        public async Task<IActionResult> Overview(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Step1", new { envId });
            }

            var client = await _managementClientProvider.CreateBatchManagementClient(environment.SubscriptionId);
            var (account, usage) = await (
                client.BatchAccount.GetAsync(
                    environment.BatchAccount.ResourceGroupName,
                    environment.BatchAccount.Name),
                _poolUsageProvider.GetEnvironmentUsage(environment));

            var model = new ViewEnvironmentModel(environment, account, usage);

            return View("View/Overview", model);
        }

        [HttpGet]
        [Route("Environments/{envId}/Details")]
        public async Task<IActionResult> Details(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Step1", new { envId });
            }

            var model = new ViewEnvironmentModel(environment);

            return View("View/Details", model);
        }

        [HttpGet]
        [Route("Environments/{envId}/Config")]
        public async Task<IActionResult> Config(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Step1", new { envId });
            }

            var endpoint = $"{Request.Scheme}://{Request.Host}/api/environments/{envId}/pools/{{poolId}}";

            var model = new EnvironmentConfigurationModel(environment, _startTaskProvider, endpoint);

            return View("View/Config", model);
        }

        [HttpPost]
        [Route("Environments/{envId}/Config")]
        public async Task<IActionResult> Config(string envId, EnvironmentConfigurationModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("View/Config", model);
            }

            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Step1", new { envId });
            }

            if (environment.AutoScaleConfiguration == null)
            {
                environment.AutoScaleConfiguration =  new AutoScaleConfiguration();
            }

            environment.AutoScaleConfiguration.MaxIdleCpuPercent = model.MaxIdleCpuPercent;
            environment.AutoScaleConfiguration.MaxIdleGpuPercent = model.MaxIdleGpuPercent;

            environment.AutoScaleConfiguration.SpecificProcesses =
                string.IsNullOrWhiteSpace(model.SpecificProcesses) ?
                    null :
                    model.SpecificProcesses.Split(',').ToList();

            environment.AutoScaleConfiguration.ScaleEndpointEnabled = model.ScaleEndpointEnabled;

            var primaryKeyToUse = string.IsNullOrEmpty(environment.AutoScaleConfiguration.PrimaryApiKey)
                ? GenerateApiKey()
                : environment.AutoScaleConfiguration.PrimaryApiKey;
            environment.AutoScaleConfiguration.PrimaryApiKey = model.ScaleEndpointEnabled ? primaryKeyToUse : null;

            var secondaryKeyToUse = string.IsNullOrEmpty(environment.AutoScaleConfiguration.SecondaryApiKey)
                ? GenerateApiKey()
                : environment.AutoScaleConfiguration.SecondaryApiKey;
            environment.AutoScaleConfiguration.SecondaryApiKey = model.ScaleEndpointEnabled ? secondaryKeyToUse : null;

            // Start task scripts
            environment.WindowsBootstrapScript = model.WindowsBootstrapScript;
            environment.LinuxBootstrapScript = model.LinuxBootstrapScript;

            await _environmentCoordinator.UpdateEnvironment(environment);

            return RedirectToAction("Config", new { envId = environment.Name });
        }

        private string GenerateApiKey()
        {
            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[64];
                rng.GetBytes(tokenData);
                return Convert.ToBase64String(tokenData);
            }
        }

        [HttpGet]
        [Route("Environments/{envId}/Identity")]
        public async Task<IActionResult> Identity(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Step1", new { envId });
            }

            var model = new ViewEnvironmentModel(environment);
            return View("View/Identity", model);
        }

        [HttpGet]
        [Route("Environments/{envId}/ApplyPermissions")]
        public async Task<ActionResult> ApplyPermissions(string envId, ViewEnvironmentModel model)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return NotFound("No new environment configuration in progress");
            }

            try
            {
                // Add KV SP to access policies
                await (_azureResourceProvider.AddReaderIdentityToAccessPolicies(
                    environment.SubscriptionId,
                    environment.KeyVault,
                    environment.KeyVaultServicePrincipal),

                    UploadKeyVaultCertificateToBatch(environment),

                    AssignManagementIdentityToResources(environment));
            }
            catch (CloudException cEx)
            {
                ModelState.AddModelError("", $"Failed to add reader identity to Azure Key Vault with error: {cEx.Message}");
            }

            return RedirectToAction("Identity", new { envId });
        }

        [HttpGet]
        [Route("Environments/{envId}/Resources")]
        public async Task<IActionResult> Resources(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Step1", new { envId });
            }

            var model = new ViewEnvironmentModel(environment);
            return View("View/Resources", model);
        }

        [HttpGet]
        [Route("Environments/{envId}/Manager")]
        public async Task<IActionResult> Manager(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Step1", new { envId });
            }

            var model = new ViewEnvironmentModel(environment);
            return View("View/Manager", model);
        }

        [HttpPost]
        [Route("Environments/{envId}/Manager")]
        public async Task<IActionResult> Manager(string envId, ViewEnvironmentModel model)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Step1", new { envId });
            }

            if (environment.RenderManager == RenderManagerType.Deadline)
            {
                if (string.IsNullOrWhiteSpace(model.DeadlineEnvironment?.WindowsDeadlineRepositoryShare))
                {
                    ModelState.AddModelError(nameof(DeadlineEnvironment.WindowsDeadlineRepositoryShare), "The Deadline respoitory server cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(model.DeadlineEnvironment?.LicenseMode))
                {
                    ModelState.AddModelError(nameof(DeadlineEnvironment.LicenseMode), "The Deadline license mode cannot be empty.");
                }

                // If they specify standard licensing, ensure there's a license server.
                if (!string.IsNullOrWhiteSpace(model.DeadlineEnvironment?.LicenseMode) &&
                    model.DeadlineEnvironment.LicenseMode == LicenseMode.Standard.ToString() &&
                    string.IsNullOrWhiteSpace(model.DeadlineEnvironment?.LicenseServer))
                {
                    ModelState.AddModelError(nameof(DeadlineEnvironment.LicenseServer), "The Deadline license server cannot be empty when using standard licensing.");
                }

                if (model.DeadlineEnvironment != null && model.DeadlineEnvironment.RunAsService)
                {
                    if (string.IsNullOrWhiteSpace(model.DeadlineEnvironment.ServiceUser))
                    {
                        ModelState.AddModelError(nameof(DeadlineEnvironment.ServiceUser), "The service user must be specified when running the Deadline client as a service.");
                    }

                    if (string.IsNullOrWhiteSpace(model.DeadlineEnvironment.ServicePassword))
                    {
                        ModelState.AddModelError(nameof(DeadlineEnvironment.ServicePassword), "The service password must be specified when running the Deadline client as a service.");
                    }
                }
            }

            if (environment.RenderManager == RenderManagerType.Qube610 || environment.RenderManager == RenderManagerType.Qube70)
            {
                if (string.IsNullOrWhiteSpace(model.QubeEnvironment?.QubeSupervisor))
                {
                    ModelState.AddModelError(nameof(QubeEnvironment.QubeSupervisor), $"The Qube supervisor cannot be empty.");
                }
            }

            if (environment.RenderManager == RenderManagerType.Tractor)
            {
                if (string.IsNullOrWhiteSpace(model.TractorEnvironment?.TractorSettings))
                {
                    ModelState.AddModelError(nameof(TractorEnvironment.TractorSettings), $"The Tractor manager cannot be empty.");
                }
            }

            if (model.JoinDomain)
            {
                if (string.IsNullOrWhiteSpace(model.DomainName))
                {
                    ModelState.AddModelError(nameof(ViewEnvironmentModel.DomainName), $"A domain name must be specified if joining a domain.");
                }

                if (string.IsNullOrWhiteSpace(model.DomainJoinUsername))
                {
                    ModelState.AddModelError(nameof(ViewEnvironmentModel.DomainJoinUsername), $"A domain user must be specified if joining a domain.");
                }

                // Ensure there's an existing password or one specified
                if ((environment.Domain == null || string.IsNullOrWhiteSpace(environment.Domain.DomainJoinPassword)) && string.IsNullOrWhiteSpace(model.DomainJoinPassword))
                {
                    ModelState.AddModelError(nameof(ViewEnvironmentModel.DomainJoinPassword), $"A domain password must be specified if joining a domain.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View("View/Manager", model);
            }

            if (environment.RenderManager == RenderManagerType.Deadline)
            {
                ApplyDeadlineConfigToEnvironment(environment, model.DeadlineEnvironment);
            }

            if (environment.RenderManager == RenderManagerType.Qube610 || environment.RenderManager == RenderManagerType.Qube70)
            {
                environment.RenderManagerConfig.Qube.SupervisorIp = model.QubeEnvironment.QubeSupervisor;
            }

            if (environment.RenderManager == RenderManagerType.Tractor)
            {
                environment.RenderManagerConfig.Tractor.TractorSettings = model.TractorEnvironment.TractorSettings;
            }

            environment.Domain = model.JoinDomain ? new DomainConfig() : null;

            if (model.JoinDomain)
            {
                environment.Domain.JoinDomain = model.JoinDomain;
                environment.Domain.DomainName = model.DomainName;
                environment.Domain.DomainWorkerOuPath = model.DomainWorkerOuPath;
                environment.Domain.DomainJoinUsername = model.DomainJoinUsername;
                environment.Domain.DomainJoinPassword = model.DomainJoinPassword;
            }

            await _environmentCoordinator.UpdateEnvironment(environment);

            return View("View/Manager", model);
        }

        private void ApplyDeadlineConfigToEnvironment(RenderingEnvironment environment, DeadlineEnvironment model)
        {
            if (environment.RenderManagerConfig.Deadline == null)
            {
                environment.RenderManagerConfig.Deadline = new DeadlineConfig();
            }

            environment.RenderManagerConfig.Deadline.WindowsRepositoryPath = model.WindowsDeadlineRepositoryShare;
            environment.RenderManagerConfig.Deadline.RepositoryUser = model.RepositoryUser;
            environment.RenderManagerConfig.Deadline.RepositoryPassword = model.RepositoryPassword;
            environment.RenderManagerConfig.Deadline.LicenseServer = model.LicenseServer;
            environment.RenderManagerConfig.Deadline.DeadlineRegion = model.DeadlineRegion;
            environment.RenderManagerConfig.Deadline.ExcludeFromLimitGroups =
                string.IsNullOrWhiteSpace(model.ExcludeFromLimitGroups) ?
                    null :
                    model.ExcludeFromLimitGroups.Replace(" ", "");

            LicenseMode mode;
            Enum.TryParse<LicenseMode>(model.LicenseMode, out mode);
            environment.RenderManagerConfig.Deadline.LicenseMode = mode;

            environment.RenderManagerConfig.Deadline.RunAsService = model.RunAsService;
            environment.RenderManagerConfig.Deadline.ServiceUser = model.RunAsService ? model.ServiceUser : null;
            environment.RenderManagerConfig.Deadline.ServicePassword = model.RunAsService ? model.ServicePassword : null;

            if (model.DeadlineDatabaseCertificate != null && model.DeadlineDatabaseCertificate.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    model.DeadlineDatabaseCertificate.CopyTo(ms);
                    environment.RenderManagerConfig.Deadline.DeadlineDatabaseCertificate = new Certificate
                    {
                        CertificateData = ms.ToArray()
                    };
                }
            }

            environment.RenderManagerConfig.Deadline.DeadlineDatabaseCertificate.Password =
                model.DeadlineDatabaseCertificatePassword;
        }

        [HttpGet]
        [Route("Environments/{envId}/Storage")]
        public async Task<IActionResult> Storage(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                return RedirectToAction("Step1", new { envId });
            }

            var storageProps = await _azureResourceProvider.GetStorageProperties(
                environment.StorageAccount.SubscriptionId,
                environment.StorageAccount.ResourceGroupName,
                environment.StorageAccount.Name);

            var model = new EnvironmentStorageConfigModel(environment.RenderManager, storageProps)
            {
                EnvironmentName = envId
            };

            return View("View/Storage", model);
        }

        [HttpGet]
        [Route("Environments/New")]
        [Route("Environments/Step1/{envId?}")]
        public async Task<ActionResult> Step1(string envId)
        {
            var model = new AddEnvironmentStep1Model();

            if (!string.IsNullOrEmpty(envId))
            {
                var environment = await _environmentCoordinator.GetEnvironment(envId);
                if (environment != null)
                {
                    model = new AddEnvironmentStep1Model(environment);
                }
                else
                {
                    // most likely been automatically redirected here
                    model.EnvironmentName = envId;
                }
            }

            if (!model.SubscriptionId.HasValue)
            {
                model.SubscriptionId = Guid.Parse(_configuration["SubscriptionId"]);
            }

            return View("Create/Step1", model);
        }

        [HttpPost]
        public async Task<ActionResult> Step1(AddEnvironmentStep1Model model)
        {
            if (!ModelState.IsValid)
            {
                return View("Create/Step1", model);
            }

            // always get the environment with the originally set name
            var environment = await _environmentCoordinator.GetEnvironment(model.OriginalName ?? model.EnvironmentName)
                ?? new RenderingEnvironment { InProgress = true };

            if (!model.SubscriptionId.HasValue)
            {
                ModelState.AddModelError(nameof(AddEnvironmentStep1Model.SubscriptionId), "No subscription ID was supplied. Subscription ID is a required field.");
                return View("Create/Step1", model);
            }

            environment.Name = model.EnvironmentName;
            environment.SubscriptionId = model.SubscriptionId.Value;
            environment.LocationName = model.LocationName;
            environment.RenderManager = model.RenderManager.Value;

            await _environmentCoordinator.UpdateEnvironment(environment, model.OriginalName);

            return RedirectToAction("Step2", new { envId = environment.Name });
        }

        [HttpGet]
        [Route("Environments/Step3/{envId?}")]
        public async Task<ActionResult> Step3(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                // redirect to Step1 if no config.
                return RedirectToAction("Step1", new { envId });
            }

            var model = new AddEnvironmentStep3Model(environment);

            var canAssign = await _azureResourceProvider.CanCreateRoleAssignments(environment.SubscriptionId, environment.ResourceGroupName);
            if (!canAssign)
            {
                model.Error = "You don't have the required permissions to assign roles to users";
                model.ErrorMessage = "In order to complete this step which involves creating role assignments, you must have the Owner or User Access Administrator role for the specified Subscription. " +
                                     "Either request someone with this role to complete the step, or ask your admin to make you an Owner or User Access Administrator for the Subscription or Resource Group.";
            }

            return View("Create/Step3", model);
        }

        [HttpPost]
        public async Task<ActionResult> Step3(AddEnvironmentStep3Model model)
        {
            if (!model.KeyVaultServicePrincipalAppId.HasValue)
            {
                ModelState.AddModelError(nameof(AddEnvironmentStep3Model.KeyVaultServicePrincipalAppId), "KeyVaultServicePrincipalAppId is a required field.");
            }

            if (!ModelState.IsValid)
            {
                return View("Create/Step3", model);
            }

            var environment = await _environmentCoordinator.GetEnvironment(model.EnvironmentName);
            if (environment == null)
            {
                return BadRequest("No new environment configuration in progress");
            }

            if (!model.KeyVaultServicePrincipalAppId.HasValue ||
                !model.KeyVaultServicePrincipalObjectId.HasValue)
            {
                // shouldn't happen as the validation above will catch this first
                return BadRequest("KVSP is not supplied");
            }

            var tenantId = Guid.Parse(_configuration.GetSection("AzureAd")["TenantId"]);

            environment.KeyVaultServicePrincipal = new ServicePrincipal
            {
                ApplicationId = model.KeyVaultServicePrincipalAppId.Value,
                ObjectId = model.KeyVaultServicePrincipalObjectId.Value,
                CertificateKeyVaultName = model.KeyVaultServicePrincipalCertificateName,
                TenantId = tenantId,
            };

            try
            {
                // Add KV SP to access policies
                await (_azureResourceProvider.AddReaderIdentityToAccessPolicies(
                    environment.SubscriptionId,
                    environment.KeyVault,
                    environment.KeyVaultServicePrincipal),

                    UploadKeyVaultCertificateToBatch(environment),

                    AssignManagementIdentityToResources(environment));
            }
            catch (CloudException cEx)
            {
                ModelState.AddModelError("", $"Failed to add reader identity to Azure Key Vault with error: {cEx.Message}");
                return View("Create/Step3", model);
            }
            finally
            {
                await _environmentCoordinator.UpdateEnvironment(environment);
            }

            return RedirectToAction("Step4", new { envId = environment.Name });
        }

        [HttpGet]
        [Route("Environments/Step2/{envId?}")]
        public async Task<ActionResult> Step2(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                // redirect to Step1 if no config.
                return RedirectToAction("Step1", new { envId });
            }

            var model = new AddEnvironmentStep2Model(environment);

            var canCreate = await _azureResourceProvider.CanCreateResources(environment.SubscriptionId);
            if (!canCreate)
            {
                model.Error = "You don't have the required permissions to create resources";
                model.ErrorMessage = "In order to complete this step which involves creating resources, you must have the Owner or Contributor role for the specified Subscription. " +
                                     "Either request someone with this role to complete the step, or ask your admin to make you an Owner or Contributor for the Subscription.";
            }

            return View("Create/Step2", model);
        }

        [HttpPost]
        [Route("Environments/Step2/{envId}")]
        public async Task<ActionResult> Step2(string envId, AddEnvironmentStep2Model model)
        {
            // TODO: Move this into a step 2 validator ... maybe
            if (!NewOrExistingFieldValid(model.BatchAccountResourceIdLocationUrl, model.NewBatchAccountName))
            {
                ModelState.AddModelError(nameof(AddEnvironmentStep2Model.BatchAccountResourceIdLocationUrl), "Either an existing or new Batch account should be supplied");
            }

            if (!NewOrExistingFieldValid(model.StorageAccountResourceIdAndLocation, model.NewStorageAccountName))
            {
                ModelState.AddModelError(nameof(AddEnvironmentStep2Model.StorageAccountResourceIdAndLocation), "Either an existing or new Storage account should be supplied");
            }

            if (!NewOrExistingFieldValid(model.SubnetResourceIdLocationAndAddressPrefix, model.NewVnetName))
            {
                ModelState.AddModelError(nameof(AddEnvironmentStep2Model.SubnetResourceIdLocationAndAddressPrefix), "Either an existing or new VNet name should be supplied");
            }

            if (!NewOrExistingFieldValid(model.ApplicationInsightsIdAndLocation, model.NewApplicationInsightsName))
            {
                ModelState.AddModelError(nameof(AddEnvironmentStep2Model.ApplicationInsightsIdAndLocation), "Either an existing or new Application Insights name should be supplied");
            }

            var environment = await _environmentCoordinator.GetEnvironment(model.EnvironmentName);
            if (environment == null)
            {
                return BadRequest("No new environment configuration in progress");
            }

            // UI should stop this now, but we shouldn't trust the UI.
            if (NewOrExistingFieldValid(model.BatchAccountResourceIdLocationUrl))
            {
                // check the batch account and environment are in the same location
                var batchAccountLocation = model.BatchAccountResourceIdLocationUrl.Split(";")[1];
                if (false == batchAccountLocation.Equals(environment.LocationName, StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(nameof(AddEnvironmentStep2Model.BatchAccountResourceIdLocationUrl), $"Environment and Batch account must be configured to the same location: ({environment.LocationName})");
                }
            }

            if (NewOrExistingFieldValid(model.SubnetResourceIdLocationAndAddressPrefix))
            {
                // check the vNet and environment are in the same location
                var subnetLocation = model.SubnetResourceIdLocationAndAddressPrefix.Split(";")[1];
                if (false == subnetLocation.Equals(environment.LocationName, StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(nameof(AddEnvironmentStep2Model.SubnetResourceIdLocationAndAddressPrefix), $"Environment and Subnet must be configured to the same location: ({environment.LocationName})");
                }
            }

            if (environment.KeyVault == null)
            {
                // Ensure it doesn't exist if it hasn't been created already
                var nameAvailability = await _azureResourceProvider.ValidateKeyVaultName(environment.SubscriptionId, model.KeyVaultName);
                if (!nameAvailability.NameAvailable.GetValueOrDefault(false))
                {
                    ModelState.AddModelError(nameof(AddEnvironmentStep2Model.KeyVaultName), $"The Key Vault name is not available. {nameAvailability.Message}");
                    return View("Create/Step2", model);
                }
            }

            if (!ModelState.IsValid)
            {
                // disabled select inputs will be null if disabled
                model.NewBatchAccountVisible = string.IsNullOrEmpty(model.BatchAccountResourceIdLocationUrl);
                model.NewStorageAccountVisible = string.IsNullOrEmpty(model.StorageAccountResourceIdAndLocation);
                model.NewAppInsightsVisible = string.IsNullOrEmpty(model.ApplicationInsightsIdAndLocation);
                model.NewVNetVisible = string.IsNullOrEmpty(model.SubnetResourceIdLocationAndAddressPrefix);

                return View("Create/Step2", model);
            }

            try
            {
                // TODO Replace with ARM Template
                await CreateOrUpdateResourceGroup(environment, model);
                await (CreateOrUpdateKeyVault(environment, model),
                    CreateOrUpdateStorageAndBatchAccounts(environment, model),
                    CreateOrUpdateVnet(environment, model),
                    CreateOrUpdateAppInsights(environment, model));
            }
            catch (CloudException ex)
            {
                ModelState.AddModelError("", $"{ex.Body.Message} ({ex.Body.Code})");
                ModelState.AddModelError("", $"{ex}");
                return View("Create/Step2", model);
            }
            catch (Exception ex)
            {
                // TODO: would be good to know which one actually failed, but hopefully the answer is in the error.
                ModelState.AddModelError("", $"Failed to create or update account, storage, app insights, or vnet: {ex}");
                return View("Create/Step2", model);
            }
            finally
            {
                await _environmentCoordinator.UpdateEnvironment(environment);
            }

            return RedirectToAction("Step3", new { envId = environment.Name });
        }

        [HttpGet]
        [Route("Environments/Step4/{envId?}")]
        public async Task<ActionResult> Step4(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                // redirect to Step1 if no config.
                return RedirectToAction("Step1", new { envId });
            }

            var model = new AddEnvironmentStep4Model(environment);
            return View("Create/Step4", model);
        }

        [HttpPost]
        public async Task<ActionResult> Step4(AddEnvironmentStep4Model model)
        {
            if (model.JoinDomain)
            {
                if (string.IsNullOrWhiteSpace(model.DomainName))
                {
                    ModelState.AddModelError(nameof(ViewEnvironmentModel.DomainName), $"A domain name must be specified if joining a domain.");
                }

                if (string.IsNullOrWhiteSpace(model.DomainJoinUsername))
                {
                    ModelState.AddModelError(nameof(ViewEnvironmentModel.DomainJoinUsername), $"A domain user must be specified if joining a domain.");
                }

                if (string.IsNullOrWhiteSpace(model.DomainJoinPassword))
                {
                    ModelState.AddModelError(nameof(ViewEnvironmentModel.DomainJoinPassword), $"A domain password must be specified if joining a domain.");
                }
            }

            var environment = await _environmentCoordinator.GetEnvironment(model.EnvironmentName);
            if (environment == null)
            {
                return RedirectToAction("Step1", new { model.EnvironmentName });
            }

            if (environment.RenderManager == RenderManagerType.Deadline)
            {
                if (string.IsNullOrWhiteSpace(model.DeadlineEnvironment?.WindowsDeadlineRepositoryShare))
                {
                    ModelState.AddModelError(nameof(DeadlineEnvironment.WindowsDeadlineRepositoryShare), $"The Deadline respoitory server cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(model.DeadlineEnvironment?.LicenseMode))
                {
                    ModelState.AddModelError(nameof(DeadlineEnvironment.LicenseMode), $"The Deadline license mode cannot be empty.");
                }

                // If they specify standard licensing, ensure there's a license server.
                if (!string.IsNullOrWhiteSpace(model.DeadlineEnvironment?.LicenseMode) &&
                    model.DeadlineEnvironment.LicenseMode == LicenseMode.Standard.ToString() &&
                    string.IsNullOrWhiteSpace(model.DeadlineEnvironment?.LicenseServer))
                {
                    ModelState.AddModelError(nameof(DeadlineEnvironment.LicenseServer), $"The Deadline license server cannot be empty when using standard licensing.");
                }

                if (model.DeadlineEnvironment != null && model.DeadlineEnvironment.RunAsService)
                {
                    if (string.IsNullOrWhiteSpace(model.DeadlineEnvironment.ServiceUser))
                    {
                        ModelState.AddModelError(nameof(DeadlineEnvironment.ServiceUser), "The service user must be specified when running the Deadline client as a service.");
                    }

                    if (string.IsNullOrWhiteSpace(model.DeadlineEnvironment.ServicePassword))
                    {
                        ModelState.AddModelError(nameof(DeadlineEnvironment.ServicePassword), "The service password must be specified when running the Deadline client as a service.");
                    }
                }
            }

            if (environment.RenderManager == RenderManagerType.Qube610 || environment.RenderManager == RenderManagerType.Qube70)
            {
                if (string.IsNullOrWhiteSpace(model.QubeEnvironment?.QubeSupervisor))
                {
                    ModelState.AddModelError(nameof(QubeEnvironment.QubeSupervisor), $"The Qube supervisor cannot be empty.");
                }
            }

            if (environment.RenderManager == RenderManagerType.Tractor)
            {
                if (string.IsNullOrWhiteSpace(model.TractorEnvironment?.TractorSettings))
                {
                    ModelState.AddModelError(nameof(TractorEnvironment.TractorSettings), $"The Tractor manager cannot be empty.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View("Create/Step4", model);
            }

            if (environment.RenderManagerConfig == null)
            {
                environment.RenderManagerConfig = new RenderManagerConfig();
            }

            environment.Domain = model.JoinDomain ? new DomainConfig() : null;

            if (model.JoinDomain)
            {
                environment.Domain.JoinDomain = model.JoinDomain;
                environment.Domain.DomainName = model.DomainName;
                environment.Domain.DomainWorkerOuPath = model.DomainWorkerOuPath;
                environment.Domain.DomainJoinUsername = model.DomainJoinUsername;
                environment.Domain.DomainJoinPassword = model.DomainJoinPassword;
            }

            if (model.DeadlineEnvironment != null)
            {
                ApplyDeadlineConfigToEnvironment(environment, model.DeadlineEnvironment);
            }

            if (model.QubeEnvironment != null)
            {
                if (environment.RenderManagerConfig.Qube == null)
                {
                    environment.RenderManagerConfig.Qube = new QubeConfig();
                }

                environment.RenderManagerConfig.Qube.SupervisorIp = model.QubeEnvironment.QubeSupervisor;
            }

            // TODO: Added this one just to complete the available options. Probably needs to change.

            if (model.TractorEnvironment != null)
            {
                if (environment.RenderManagerConfig.Tractor == null)
                {
                    environment.RenderManagerConfig.Tractor = new TractorConfig();
                }

                environment.RenderManagerConfig.Tractor.TractorSettings = model.TractorEnvironment.TractorSettings;
            }

            // TODO Re-enable package upload step
            // return RedirectToAction("Step5", new { envId = environment.Name });

            environment.InProgress = false;
            await _environmentCoordinator.UpdateEnvironment(environment);
            // after saving, either go to overview details, or a success page.
            return RedirectToAction("Overview", new { envId = model.EnvironmentName });
        }

        [HttpGet]
        [Route("Environments/Step5/{envId?}")]
        public async Task<ActionResult> Step5(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                // redirect to Step1 if no config.
                return RedirectToAction("Step1", new { envId });
            }

            var model = new AddEnvironmentFinalizeModel(environment);
            return View("Create/Step5", model);
        }

        [HttpPost]
        public async Task<ActionResult> Step5(AddEnvironmentFinalizeModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Create/Step5", model);
            }

            var environment = await _environmentCoordinator.GetEnvironment(model.EnvironmentName);
            if (environment == null)
            {
                return BadRequest("No new environment configuration in progress");
            }

            // todo: set what we need to here
            // await UpdateEnvironment(environment);

            return RedirectToAction("Step6", new { envId = environment.Name });
        }

        [HttpGet]
        [Route("Environments/Step6/{envId?}")]
        public async Task<ActionResult> Step6(string envId)
        {
            var environment = await _environmentCoordinator.GetEnvironment(envId);
            if (environment == null)
            {
                // redirect to Step1 if no config.
                return RedirectToAction("Step1", new { envId });
            }

            var model = new AddEnvironmentFinalizeModel(environment);
            return View("Create/Step6", model);
        }

        [HttpPost]
        public async Task<ActionResult> Step6(AddEnvironmentFinalizeModel model)
        {
            if (!ModelState.IsValid)
            {
                // return to last step. actually this could be step 4, 5, or 6.
                return View("Create/Step6", model);
            }

            var environment = await _environmentCoordinator.GetEnvironment(model.EnvironmentName);
            if (environment == null)
            {
                return BadRequest("No new environment configuration in progress");
            }

            // todo: set what we need to here
            environment.InProgress = false;
            await _environmentCoordinator.UpdateEnvironment(environment);

            // after saving, either go to overview details, or a success page.
            return RedirectToAction("Details", new { envId = model.EnvironmentName });
        }

        private bool NewOrExistingFieldValid(string existingId, string newId = null)
        {
            // Can't use string.Contains("existing"). Account might be "my-existing-account"
            if (string.IsNullOrWhiteSpace(newId))
            {
                // validate existingId
                return !string.IsNullOrWhiteSpace(existingId) && existingId != "#";
            }

            if (string.IsNullOrWhiteSpace(existingId) || existingId == "#")
            {
                // validate newId
                return !string.IsNullOrWhiteSpace(newId);
            }


            return true;
        }

        private async Task CreateOrUpdateStorageAndBatchAccounts(RenderingEnvironment environment, AddEnvironmentStep2Model model)
        {
            if (!string.IsNullOrEmpty(model.NewStorageAccountName))
            {
                environment.StorageAccount = await _azureResourceProvider.CreateStorageAccountAsync(
                    model.SubscriptionId,
                    environment.LocationName,
                    environment.ResourceGroupName,
                    model.NewStorageAccountName,
                    environment.Name);
            }
            else
            {
                environment.StorageAccount = new StorageAccount
                {
                    ResourceId = model.StorageAccountResourceIdAndLocation.Split(";")[0],
                    Location = model.StorageAccountResourceIdAndLocation.Split(";")[1],
                };
            }

            await _azureResourceProvider.CreateFilesShare(
                environment.StorageAccount.SubscriptionId,
                environment.StorageAccount.ResourceGroupName,
                environment.StorageAccount.Name,
                model.NewFileShareName);

            if (!string.IsNullOrEmpty(model.NewBatchAccountName))
            {
                environment.BatchAccount = await _azureResourceProvider.CreateBatchAccountAsync(
                    model.SubscriptionId,
                    environment.LocationName,
                    environment.ResourceGroupName,
                    model.NewBatchAccountName,
                    environment.StorageAccount.ResourceId,
                    environment.Name);
            }
            else
            {
                environment.BatchAccount = new BatchAccount
                {
                    ResourceId = model.BatchAccountResourceIdLocationUrl.Split(";")[0],
                    Location = model.BatchAccountResourceIdLocationUrl.Split(";")[1],
                    Url = $"https://{model.BatchAccountResourceIdLocationUrl.Split(";")[2]}",
                };
            }
        }

        private async Task UploadKeyVaultCertificateToBatch(RenderingEnvironment environment)
        {
            var password = Guid.NewGuid().ToString();
            var certificate = await _keyVaultMsiClient.GetKeyVaultCertificateAsync(
                environment.SubscriptionId,
                environment.KeyVault,
                environment.KeyVaultServicePrincipal.CertificateKeyVaultName,
                password);

            if (certificate == null)
            {
                throw new Exception("The Key Vault service principal doesn't exist in Key Vault.  " +
                                    $"If you're using an existing SP, did you upload the certificate to {environment.KeyVaultServicePrincipal.CertificateKeyVaultName}?");
            }

            await _azureResourceProvider.UploadCertificateToBatchAccountAsync(
                environment.SubscriptionId,
                environment.BatchAccount,
                certificate,
                password);

            // Save the thumbprint as it's needed when we create the pool.
            environment.KeyVaultServicePrincipal.Thumbprint = certificate.Thumbprint;
        }

        private async Task CreateOrUpdateVnet(RenderingEnvironment environment, AddEnvironmentStep2Model model)
        {
            if (!string.IsNullOrEmpty(model.NewVnetName))
            {
                // TODO - Expose this
                var vnetAddressPrefix = "10.8.0.0/16";
                var subnetAddressRange = "10.8.0.0/24";

                environment.Subnet = await _azureResourceProvider.CreateVnetAsync(
                    model.SubscriptionId,
                    environment.LocationName,
                    environment.ResourceGroupName,
                    model.NewVnetName,
                    "subnet",
                    vnetAddressPrefix,
                    subnetAddressRange,
                    environment.Name);
            }
            else
            {
                var subnet = new Subnet
                {
                    ResourceId = model.SubnetResourceIdLocationAndAddressPrefix.Split(";")[0],
                    Location = model.SubnetResourceIdLocationAndAddressPrefix.Split(";")[1],
                    AddressPrefix = model.SubnetResourceIdLocationAndAddressPrefix.Split(";")[2],
                };

                if (environment.Subnet == null || environment.Subnet.ResourceId != subnet.ResourceId)
                {
                    environment.Subnet = await _azureResourceProvider.GetVnetAsync(
                        model.SubscriptionId,
                        subnet.Location,
                        subnet.ResourceGroupName,
                        subnet.VNetName,
                        subnet.Name);
                }
            }
        }

        private async Task CreateOrUpdateResourceGroup(RenderingEnvironment environment, AddEnvironmentStep2Model model)
        {
            try
            {
                await _azureResourceProvider.CreateResourceGroupAsync(
                    environment.SubscriptionId,
                    environment.LocationName,
                    environment.ResourceGroupName,
                    environment.Name);
            }
            catch (CloudException cEx)
            {
                ModelState.AddModelError("", $"Failed to create Resource Group with error: {cEx.Message}");
                throw;
            }
        }

        private async Task CreateOrUpdateKeyVault(RenderingEnvironment environment, AddEnvironmentStep2Model model)
        {
            try
            {
                if (environment.KeyVault == null)
                {
                    var keyVaultResponse = await _azureResourceProvider.CreateKeyVaultAsync(
                        _identityProvider.GetPortalManagedServiceIdentity(),
                        _identityProvider.GetCurrentUserIdentity(HttpContext),
                        environment.SubscriptionId,
                        environment.ResourceGroupName,
                        environment.LocationName,
                        model.KeyVaultName,
                        environment.Name);

                    environment.KeyVault = new KeyVault
                    {
                        ResourceId = keyVaultResponse.Id,
                        Uri = keyVaultResponse.Properties.VaultUri,
                        ExistingResource = false,
                    };
                }
            }
            catch (CloudException cEx)
            {
                ModelState.AddModelError("", $"Failed to create Azure Key Vault with error: {cEx.Message}");
                if (cEx.Response?.StatusCode == HttpStatusCode.Conflict && cEx.Body?.Code == "VaultAlreadyExists")
                {
                    ModelState.AddModelError(nameof(AddEnvironmentStep2Model.KeyVaultName), "The Key Vault name already exists, please choose a different name. It may be that the KeyVault exists in another Subscription or Location.");
                }

                throw;
            }
        }

        private async Task CreateOrUpdateAppInsights(RenderingEnvironment environment, AddEnvironmentStep2Model model)
        {
            string appInsightsName;
            string appInsightsLocation;
            string appInsightsResourceGroup;

            if (!string.IsNullOrEmpty(model.NewApplicationInsightsName))
            {
                appInsightsName = model.NewApplicationInsightsName;
                appInsightsLocation = model.NewApplicationInsightsLocation;
                appInsightsResourceGroup = environment.ResourceGroupName;

                // creating a new one, in case the customer has previously assigned one, we need to remove it.
                environment.ApplicationInsightsAccount = null;
            }
            else
            {
                var appInsights = new ApplicationInsightsAccount
                {
                    ResourceId = model.ApplicationInsightsIdAndLocation.Split(";")[0],
                    Location = model.ApplicationInsightsIdAndLocation.Split(";")[1],
                };

                appInsightsName = appInsights.Name;
                appInsightsLocation = appInsights.Location;
                appInsightsResourceGroup = appInsights.ResourceGroupName;
            }

            if (environment.ApplicationInsightsAccount?.ApplicationId == null || environment.ApplicationInsightsAccount.InstrumentationKey == null)
            {
                environment.ApplicationInsightsAccount = await _azureResourceProvider.CreateApplicationInsightsAsync(
                    model.SubscriptionId,
                    appInsightsLocation,
                    appInsightsResourceGroup,
                    appInsightsName,
                    environment.Name);
            }

            if (string.IsNullOrEmpty(environment.ApplicationInsightsAccount.ApiKey))
            {
                environment.ApplicationInsightsAccount.ApiKey = await _azureResourceProvider.CreateApplicationInsightsApiKey(
                    model.SubscriptionId,
                    environment.ApplicationInsightsAccount,
                    Guid.NewGuid().ToString());
            }
        }

        private async Task AssignManagementIdentityToResources(RenderingEnvironment environment)
        {
            await (
            _azureResourceProvider.AssignManagementIdentityAsync(
                environment.SubscriptionId,
                environment.ResourceGroupResourceId,
                AzureResourceProvider.ContributorRole,
                _identityProvider.GetPortalManagedServiceIdentity()),

            _azureResourceProvider.AssignManagementIdentityAsync(
                environment.SubscriptionId,
                environment.ApplicationInsightsAccount.ResourceId,
                AzureResourceProvider.ContributorRole,
                _identityProvider.GetPortalManagedServiceIdentity()),

            _azureResourceProvider.AssignManagementIdentityAsync(
                environment.SubscriptionId,
                environment.Subnet.VnetResourceId,
                AzureResourceProvider.VirtualMachineContributorRole,
                _identityProvider.GetPortalManagedServiceIdentity()),

            _azureResourceProvider.AssignManagementIdentityAsync(
                environment.SubscriptionId,
                environment.BatchAccount.ResourceId,
                AzureResourceProvider.ContributorRole,
                _identityProvider.GetPortalManagedServiceIdentity()),

            _azureResourceProvider.AssignManagementIdentityAsync(
                environment.SubscriptionId,
                environment.StorageAccount.ResourceId,
                AzureResourceProvider.ContributorRole,
                _identityProvider.GetPortalManagedServiceIdentity()));
        }
    }
}
