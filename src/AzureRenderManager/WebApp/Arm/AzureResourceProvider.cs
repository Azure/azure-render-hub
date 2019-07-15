// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Management.ApplicationInsights.Management;
using Microsoft.Azure.Management.ApplicationInsights.Management.Models;
using Microsoft.Azure.Management.Authorization;
using Microsoft.Azure.Management.Authorization.Models;
using Microsoft.Azure.Management.Batch;
using Microsoft.Azure.Management.Batch.Models;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.Network.Models;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Microsoft.Rest.Azure.OData;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.File;
using WebApp.Config;
using WebApp.Models.Environments;
using WebApp.Operations;
using WebApp.Code.Extensions;
using WebApp.Code.Graph;
using WebApp.Authorization;

namespace WebApp.Arm
{
    public class AzureResourceProvider : NeedsAccessToken, IAzureResourceProvider
    {
        public const string ContributorRole = "Contributor";
        public const string VirtualMachineContributorRole = "Virtual Machine Contributor";

        private static List<string> ActionsThatCanCreate = new List<string>(
            new[]
            {
                "*"
            });

        // All the actions that can create role assignments.  Lowercase to make comparison easier
        // as the role definition actions seem to be all over the place.
        private static List<string> ActionsThatCanAssign = new List<string>(
            new[]
            {
                "*",
                "microsoft.authorization/*",
                "microsoft.authorization/*/*",
                "microsoft.authorization/*/write",
                "microsoft.authorization/roleassignments/*",
                "microsoft.authorization/roleassignments/write",
            });
        
        private readonly IConfiguration _configuration;
        private readonly IGraphProvider _graphProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        public AzureResourceProvider(
            IHttpContextAccessor contextAccessor,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IGraphProvider graphProvider) : base(contextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _graphProvider = graphProvider;
        }

        public async Task<bool> CanCreateResources(
            Guid subscriptionId)
        {
            var subscriptionScope = $"/subscriptions/{subscriptionId}";

            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var authClient = new AuthorizationManagementClient(token, _httpClientFactory.CreateClient(), false) { SubscriptionId = subscriptionId.ToString() };

            var isClassicAdminTask = IsCurrentUserClassicAdministrator(authClient);
            var result = await GetRoleDefinitions(authClient, $"/subscriptions/{subscriptionId}");
            var roleDefs = result.Where(rd =>
                rd.Permissions.Any(p =>
                    p.Actions.Any(a => ActionsThatCanCreate.Contains(a.ToLower(CultureInfo.InvariantCulture))))).ToList();

            var subscriptionRoles = await GetRoleAssignmentsForCurrentUser(authClient, subscriptionScope, roleDefs);
            var isClassicAdmin = await isClassicAdminTask;

            return isClassicAdmin || subscriptionRoles.Any(ra => roleDefs.Any(rd => rd.Id == ra.RoleDefinitionId));
        }

        public async Task<bool> IsCurrentUserClassicAdministrator(Guid subscriptionId)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var authClient = new AuthorizationManagementClient(token) { SubscriptionId = subscriptionId.ToString() };
            return await IsCurrentUserClassicAdministrator(authClient);
        }

        public async Task<List<ClassicAdministrator>> ListClassicAdministrators(Guid subscriptionId)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var authClient = new AuthorizationManagementClient(token) { SubscriptionId = subscriptionId.ToString() };
            var result = await authClient.ClassicAdministrators.ListAsync();
            return result.ToList();
        }

        private async Task<bool> IsCurrentUserClassicAdministrator(AuthorizationManagementClient authClient)
        {
            var result = await authClient.ClassicAdministrators.ListAsync();
            var classicAdmins = result.ToList();
            var user = GetUser();
            var names = user.Identities.Select(i => i.Claims.GetName()).ToList();
            var emails = user.Identities.Select(i => i.Claims.GetEmailAddress()).ToList();
            var upns = user.Identities.Select(i => i.Claims.GetUpn()).ToList();
            foreach (var adminEmail in GetClassicAdministratorEmails(classicAdmins))
            {
                if (names.Contains(adminEmail) || upns.Contains(adminEmail) || emails.Contains(adminEmail))
                {
                    return true;
                }
            }
            return false;
        }

        private IEnumerable<string> GetClassicAdministratorEmails(List<ClassicAdministrator> admins)
        {
            return admins.Where(a => a.Role.Contains("ServiceAdministrator") || a.Role.Contains("CoAdministrator")).Select(a => a.EmailAddress);
        }

        public async Task<bool> CanCreateRoleAssignments(
            Guid subscriptionId,
            string resourceGroupName)
        {
            var subscriptionScope = $"/subscriptions/{subscriptionId}";
            var resourceGroupScope = $"{subscriptionScope}/resourceGroups/{resourceGroupName}";

            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var authClient = new AuthorizationManagementClient(token, _httpClientFactory.CreateClient(), false) { SubscriptionId = subscriptionId.ToString() };

            var isClassicAdminTask = IsCurrentUserClassicAdministrator(authClient);
            var result = await GetRoleDefinitions(authClient, $"/subscriptions/{subscriptionId}");
            var roleDefs = result.Where(rd =>
                rd.Permissions.Any(p =>
                    p.Actions.Any(a => ActionsThatCanAssign.Contains(a.ToLower(CultureInfo.InvariantCulture))) &&
                    p.NotActions.All(na => !ActionsThatCanAssign.Contains(na.ToLower(CultureInfo.InvariantCulture))))).ToList();

            var subscriptionRolesTask = GetRoleAssignmentsForCurrentUser(authClient, subscriptionScope, roleDefs);
            var resourceGroupRoles = await GetRoleAssignmentsForCurrentUser(authClient, resourceGroupScope, roleDefs);
            var subscriptionRoles = await subscriptionRolesTask;
            var isClassicAdmin = await isClassicAdminTask;

            return isClassicAdmin || 
                subscriptionRoles.Any(ra => roleDefs.Any(rd => rd.Id == ra.RoleDefinitionId)) ||
                resourceGroupRoles.Any(ra => roleDefs.Any(rd => rd.Id == ra.RoleDefinitionId));
        }

        public async Task<ResourceGroup> CreateResourceGroupAsync(
            Guid subscriptionId,
            string location,
            string resourceGroupName,
            string environmentName)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var resourceClient = new ResourceManagementClient(token, _httpClientFactory.CreateClient(), false) { SubscriptionId = subscriptionId.ToString() };
            return await resourceClient.ResourceGroups.CreateOrUpdateAsync(
                resourceGroupName,
                new ResourceGroup(location, tags: GetEnvironmentTags(environmentName)));
        }

        public async Task<List<GenericResource>> ListResourceGroupResources(string subscriptionId, string rgName)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var resourceClient = new ResourceManagementClient(token, _httpClientFactory.CreateClient(), false) { SubscriptionId = subscriptionId };
            var resources = await resourceClient.Resources.ListByResourceGroupAsync(rgName);

            return resources.ToList();
        }

        public async Task DeleteResourceGroupAsync(Guid subscriptionId, string resourceGroupName)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var resourceClient = new ResourceManagementClient(token, _httpClientFactory.CreateClient(), false) { SubscriptionId = subscriptionId.ToString() };

            try
            {
                await resourceClient.ResourceGroups.BeginDeleteAsync(resourceGroupName);
            }
            catch (CloudException cEx)
            {
                if (cEx.Response?.StatusCode != HttpStatusCode.NotFound && cEx.Body?.Code != "ResourceGroupNotFound")
                {
                    throw;
                }
            }
        }

        public async Task<Microsoft.Azure.Management.KeyVault.Models.CheckNameAvailabilityResult> ValidateKeyVaultName(Guid subscriptionId, string resourceGroupName, string keyVaultName)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var kvClient = new KeyVaultManagementClient(token) { SubscriptionId = subscriptionId.ToString() };

            var param = new VaultCheckNameAvailabilityParameters { Name = keyVaultName };
            var result = await kvClient.Vaults.CheckNameAvailabilityAsync(param);
            if (!result.NameAvailable.GetValueOrDefault(true))
            {
                // The name isn't available, let's see if it's our KV
                bool available = false;
                try
                {
                    await kvClient.Vaults.GetAsync(resourceGroupName, keyVaultName);
                    available = true;
                }
                catch (CloudException ce)
                {
                    if (ce.Body.Code != "NotFound" && ce.Body.Code != "ResourceNotFound")
                    {
                        throw;
                    }
                }

                return new Microsoft.Azure.Management.KeyVault.Models.CheckNameAvailabilityResult(available, result.Reason, result.Message);
            }

            return result;
        }

        public async Task<Vault> CreateKeyVaultAsync(
            Identity.Identity portalIdentity,
            Identity.Identity ownerIdentity,
            Guid subscriptionId,
            string resourceGroupName,
            string location,
            string keyVaultName,
            string environmentName)
        {
            await RegisterProvider(subscriptionId, "Microsoft.KeyVault");

            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var kvClient = new KeyVaultManagementClient(token) { SubscriptionId = subscriptionId.ToString() };

            var permissions = new Microsoft.Azure.Management.KeyVault.Models.Permissions(
                secrets: new[] { "get", "list", "set", "delete" },
                certificates: new[] { "get", "list", "update", "delete", "create", "import" });

            var accessPolicies = new[]
            {
                // Portal MSI
                new AccessPolicyEntry(
                    portalIdentity.TenantId,
                    portalIdentity.ObjectId.ToString(),
                    permissions),

                // Owner
                new AccessPolicyEntry(
                    ownerIdentity.TenantId,
                    ownerIdentity.ObjectId.ToString(),
                    permissions)
            };

            // TODO - Make SKU configurable
            var kvParams = new VaultCreateOrUpdateParameters(
                location,
                new VaultProperties(
                    portalIdentity.TenantId,
                    new Microsoft.Azure.Management.KeyVault.Models.Sku(Microsoft.Azure.Management.KeyVault.Models.SkuName.Standard), accessPolicies),
                GetEnvironmentTags(environmentName));

            return await kvClient.Vaults.CreateOrUpdateAsync(
                resourceGroupName,
                keyVaultName,
                kvParams);
        }

        public async Task AddReaderIdentityToAccessPolicies(Guid subscriptionId, KeyVault keyVault, Config.ServicePrincipal identity)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var kvClient = new KeyVaultManagementClient(token) { SubscriptionId = subscriptionId.ToString() };

            var vault = await kvClient.Vaults.GetAsync(keyVault.ResourceGroupName, keyVault.Name);

            var accessPolicies = vault.Properties.AccessPolicies;
            if (accessPolicies.All(ap => ap.ObjectId != identity.ObjectId.ToString()))
            {
                // This identity doesn't exist, lets add it.
                accessPolicies.Add(new AccessPolicyEntry(
                    identity.TenantId,
                    identity.ObjectId.ToString(),
                    new Microsoft.Azure.Management.KeyVault.Models.Permissions(
                        secrets: new[] { "Get", "List" },
                        certificates: new[] { "Get", "List" })));

                await kvClient.Vaults.UpdateWithHttpMessagesAsync(keyVault.ResourceGroupName, keyVault.Name,
                    new VaultPatchParameters(properties: new VaultPatchProperties(accessPolicies: accessPolicies)));
            }
        }

        public async Task DeleteKeyVaultAsync(Guid subscriptionId, KeyVault keyVault)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var kvClient = new KeyVaultManagementClient(token) { SubscriptionId = subscriptionId.ToString() };

            try
            {
                await kvClient.Vaults.DeleteWithHttpMessagesAsync(keyVault.ResourceGroupName, keyVault.Name);
            }
            catch (CloudException cEx)
            {
                if (cEx.Response?.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }
        }

        public async Task<Config.StorageAccount> CreateStorageAccountAsync(
            Guid subscriptionId,
            string location,
            string resourceGroupName,
            string storageAccountName,
            string environmentName)
        {
            await RegisterProvider(subscriptionId, "Microsoft.Storage");

            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var storageClient = new StorageManagementClient(token) { SubscriptionId = subscriptionId.ToString() };

            var parameters = new StorageAccountCreateParameters(
                new Microsoft.Azure.Management.Storage.Models.Sku(Microsoft.Azure.Management.Storage.Models.SkuName.StandardLRS),
                Kind.StorageV2,
                location,
                GetEnvironmentTags(environmentName));

            var storageAccount = await storageClient.StorageAccounts.CreateAsync(
                resourceGroupName,
                storageAccountName,
                parameters);

            return new Config.StorageAccount
            {
                ResourceId = storageAccount.Id,
                Location = storageAccount.Location,
                ExistingResource = false,
            };
        }

        public async Task CreateFilesShare(
            Guid subscriptionId,
            string resourceGroupName,
            string storageAccountName,
            string filesShareName)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var storageClient = new StorageManagementClient(token) { SubscriptionId = subscriptionId.ToString() };
            var keys = await storageClient.StorageAccounts.ListKeysAsync(resourceGroupName, storageAccountName);

            var client = new CloudStorageAccount(new StorageCredentials(storageAccountName, keys.Keys.First().Value), true);
            var fileClient = client.CreateCloudFileClient();
            var share = fileClient.GetShareReference(filesShareName);
            await share.CreateIfNotExistsAsync();
        }

        public async Task<StorageProperties> GetStorageProperties(
            Guid subscriptionId,
            string resourceGroupName,
            string storageAccountName)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var storageClient = new StorageManagementClient(token) { SubscriptionId = subscriptionId.ToString() };
            var props = await storageClient.StorageAccounts.GetPropertiesAsync(resourceGroupName, storageAccountName);
            var keys = await storageClient.StorageAccounts.ListKeysAsync(resourceGroupName, storageAccountName);

            var client = new CloudStorageAccount(new StorageCredentials(storageAccountName, keys.Keys.First().Value), true);
            var fileClient = client.CreateCloudFileClient();

            var result = new StorageProperties
            {
                AccountName = storageAccountName,
                Uri = new Uri(props.PrimaryEndpoints.File),
                PrimaryKey = keys.Keys[0].Value,
                SecondaryKey = keys.Keys[1].Value,
            };

            FileContinuationToken t = null;
            do
            {
                var shares = await fileClient.ListSharesSegmentedAsync(t);

                Parallel.ForEach(shares.Results, share => share.FetchAttributesAsync());

                result.Shares.AddRange(shares.Results.Select(f => new FileShare
                {
                    Uri = f.Uri,
                    ShareName = f.Name,
                    Quota = f.Properties.Quota
                }));
                t = shares.ContinuationToken;
            } while (t != null);

            return result;
        }

        public async Task DeleteStorageAccountAsync(Guid subscriptionId, string resourceGroupName, string storageAccountName)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var storageClient = new StorageManagementClient(token) { SubscriptionId = subscriptionId.ToString() };

            try
            {
                await storageClient.StorageAccounts.DeleteAsync(resourceGroupName, storageAccountName);
            }
            catch (CloudException cEx)
            {
                if (cEx.Response?.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }
        }

        public async Task<Config.BatchAccount> CreateBatchAccountAsync(
            Guid subscriptionId,
            string location,
            string resourceGroupName,
            string batchAccountName,
            string storageAccountResourceId,
            string environmentName)
        {
            await RegisterProvider(subscriptionId, "Microsoft.Batch");

            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var batchClient = new BatchManagementClient(token) { SubscriptionId = subscriptionId.ToString() };

            var parameters = new BatchAccountCreateParameters(
                location,
                GetEnvironmentTags(environmentName),
                new AutoStorageBaseProperties(storageAccountResourceId));

            var batchAccount = await batchClient.BatchAccount.CreateAsync(resourceGroupName, batchAccountName, parameters);

            return new Config.BatchAccount
            {
                ResourceId = batchAccount.Id,
                Location = batchAccount.Location,
                Url = $"https://{batchAccount.AccountEndpoint}",
                ExistingResource = false,
            };
        }

        public async Task DeleteBatchAccountAsync(Guid subscriptionId, string resourceGroupName, string batchAccountName)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var batchClient = new BatchManagementClient(token) { SubscriptionId = subscriptionId.ToString() };

            try
            {
                await batchClient.BatchAccount.BeginDeleteAsync(resourceGroupName, batchAccountName);
            }
            catch (CloudException cEx)
            {
                if (cEx.Response?.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }
        }

        public async Task UploadCertificateToBatchAccountAsync(
            Guid subscriptionId,
            Config.BatchAccount batchAccount,
            X509Certificate2 certificate,
            string password)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var batchClient = new BatchManagementClient(token, _httpClientFactory.CreateClient(), false) { SubscriptionId = subscriptionId.ToString() };

            // Check if the cert has already been uploaded.
            var existingCerts = await batchClient.Certificate.ListByBatchAccountAsync(batchAccount.ResourceGroupName, batchAccount.Name);
            if (existingCerts != null &&
                existingCerts.Any(c =>
                    c.Thumbprint.Equals(certificate.Thumbprint, StringComparison.InvariantCultureIgnoreCase)))
            {
                return;
            }

            var parameters = new CertificateCreateOrUpdateParameters(
                Convert.ToBase64String(certificate.Export(X509ContentType.Pkcs12, password)),
                thumbprint: certificate.Thumbprint,
                thumbprintAlgorithm: "SHA1",
                password: password);

            await batchClient.Certificate.CreateAsync(
                batchAccount.ResourceGroupName,
                batchAccount.Name,
                $"SHA1-{certificate.Thumbprint}",
                parameters);
        }

        public async Task<ApplicationInsightsAccount> CreateApplicationInsightsAsync(
            Guid subscriptionId,
            string location,
            string resourceGroupName,
            string appInsightsName,
            string environmentName)
        {
            await RegisterProvider(subscriptionId, "Microsoft.Insights");

            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var appInsightsClient = new ApplicationInsightsManagementClient(token) { SubscriptionId = subscriptionId.ToString() };
            var props = new ApplicationInsightsComponent(location, "other", "other", tags: GetEnvironmentTags(environmentName));
            var appInsightsComponent =
                await appInsightsClient.Components.CreateOrUpdateAsync(resourceGroupName, appInsightsName, props);
            return new ApplicationInsightsAccount
            {
                ResourceId = appInsightsComponent.Id,
                Location = location,
                ApplicationId = appInsightsComponent.AppId,
                InstrumentationKey = appInsightsComponent.InstrumentationKey,
                ExistingResource = false,
            };
        }

        public async Task<string> CreateApplicationInsightsApiKey(
            Guid subscriptionId,
            ApplicationInsightsAccount appInsights,
            string apiKeyName)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var appInsightsClient = new ApplicationInsightsManagementClient(token) { SubscriptionId = subscriptionId.ToString() };
            var scope = $"/subscriptions/{subscriptionId}/resourcegroups/{appInsights.ResourceGroupName}/providers/Microsoft.insights/components/{appInsights.Name}/api";
            var apiKeyRequestProps = new APIKeyRequest($"RenderMgr-{apiKeyName}", new List<string>(new[] { scope }));
            var key = await appInsightsClient.APIKeys.CreateAsync(
                appInsights.ResourceGroupName,
                appInsights.Name,
                apiKeyRequestProps);
            return key.ApiKey;
        }

        public async Task DeleteApplicationInsightsAsync(Guid subscriptionId, string resourceGroupName, string appInsightsName)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var appInsightsClient = new ApplicationInsightsManagementClient(token) { SubscriptionId = subscriptionId.ToString() };

            try
            {
                await appInsightsClient.Components.DeleteAsync(resourceGroupName, appInsightsName);
            }
            catch (CloudException cEx)
            {
                if (cEx.Response?.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }
        }

        public async Task<Config.Subnet> CreateVnetAsync(
            Guid subscriptionId,
            string location,
            string resourceGroupName,
            string vnetName,
            string subnetName,
            string vnetAddressSpace,
            string subnetAddressRange,
            string environmentName)
        {
            await RegisterProvider(subscriptionId, "Microsoft.Network");

            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var networkClient = new NetworkManagementClient(token) { SubscriptionId = subscriptionId.ToString() };

            var subnets = new List<Microsoft.Azure.Management.Network.Models.Subnet>()
            {
                new Microsoft.Azure.Management.Network.Models.Subnet(name: subnetName, addressPrefix: subnetAddressRange)
            };

            var parameters = new VirtualNetwork(
                name: vnetName,
                location: location,
                addressSpace: new AddressSpace(new List<string>() {vnetAddressSpace}),
                subnets: subnets,
                tags: GetEnvironmentTags(environmentName));

            var vnet = await networkClient.VirtualNetworks.CreateOrUpdateAsync(resourceGroupName, vnetName, parameters);

            return new Config.Subnet
            {
                AddressPrefix = subnetAddressRange,
                Location = location,
                ResourceId = vnet.Subnets.First().Id,
                ExistingResource = false,
            };
        }

        public async Task<Config.Subnet> GetVnetAsync(Guid subscriptionId, string location, string resourceGroupName, string vnetName, string subnetName)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var networkClient = new NetworkManagementClient(token) { SubscriptionId = subscriptionId.ToString() };
            var vnet = await networkClient.VirtualNetworks.GetAsync(resourceGroupName, vnetName);
            return new Config.Subnet
            {
                AddressPrefix = vnet.Subnets.First(s => s.Name == subnetName).AddressPrefix,
                Location = location,
                ResourceId = vnet.Subnets.First(s => s.Name == subnetName).Id,
                ExistingResource = true,
            };
        }

        public async Task DeleteVNetAsync(Guid subscriptionId, string resourceGroupName, string vnetName)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var networkClient = new NetworkManagementClient(token) { SubscriptionId = subscriptionId.ToString() };

            try
            {
                await networkClient.VirtualNetworks.DeleteAsync(resourceGroupName, vnetName);
            }
            catch (CloudException cEx)
            {
                if (cEx.Response?.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }
        }
        
        public async Task<List<UserPermission>> GetUserPermissions(Guid subscriptionId, string scope)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var authClient = new AuthorizationManagementClient(token) { SubscriptionId = subscriptionId.ToString() };

            var roleDefinitions = await GetRoleDefinitions(authClient, scope);
            var roleDefs = roleDefinitions.ToList();

            var roleAssignments = await GetRoleAssignments(authClient, scope, roleDefs);
            roleAssignments = roleAssignments.Where(ra => ra.PrincipalType == "User").ToList();

            var userPermissions = roleAssignments.Select(ra => new UserPermission
            {
                ObjectId = ra.PrincipalId,
                Role = roleDefs.FirstOrDefault(rd => rd.Id == ra.RoleDefinitionId)?.RoleName,
                Scope = ra.Scope,
                Actions = roleDefs.FirstOrDefault(rd => rd.Id == ra.RoleDefinitionId)?.Permissions.SelectMany(p => p.Actions).ToList(),
                GraphResolutionFailure = true,
            }).ToList();

            await ResolveUsersWithGraph(userPermissions);

            return userPermissions;
        }

        private async Task ResolveUsersWithGraph(List<UserPermission> users)
        {
            var userObjectIds = users.Select(up => up.ObjectId).ToHashSet().ToList();
            var graphUsers = await _graphProvider.LookupObjectIdsAsync(GetUser(), userObjectIds);
            foreach (var user in users)
            {
                if (graphUsers.ContainsKey(user.ObjectId))
                {
                    user.Name = graphUsers[user.ObjectId].DisplayName;
                    user.Email = graphUsers[user.ObjectId].UserPrincipalName;
                    user.GraphResolutionFailure = false;
                }
            }
        }

        public async Task AssignRoleToIdentityAsync(
            Guid subscriptionId,
            string scope,
            string role,
            Identity.Identity identity)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var authClient = new AuthorizationManagementClient(token, _httpClientFactory.CreateClient(), false) { SubscriptionId = subscriptionId.ToString() };

            var result = await GetRoleDefinitions(authClient, scope);
            var roleDefs = result.Where(rd => rd.RoleName == role).ToList();
            var roleDef = roleDefs.FirstOrDefault();
            if (roleDef == null)
            {
                throw new Exception($"No {role} role definition found for resource group");
            }

            var roleAssignments = await GetRoleAssignmentsForUser(
                authClient, 
                scope, 
                identity.ObjectId.ToString(), 
                roleDefs);

            if (roleAssignments.All(ra => ra.RoleDefinitionId != roleDef.Id))
            {
                try
                {
                    await authClient.RoleAssignments.CreateAsync(
                        scope,
                        Guid.NewGuid().ToString(),
                        new RoleAssignmentCreateParameters(roleDef.Id, identity.ObjectId.ToString()));
                }
                catch (CloudException ce) when (ce.Body?.Code == "RoleAssignmentExists")
                {
                    // Ignore
                }
            }
        }

        private async Task<IPage<Microsoft.Azure.Management.Authorization.Models.RoleDefinition>> GetRoleDefinitions(
            AuthorizationManagementClient authClient,
            string scope)
        {
            var roleFilter = new ODataQuery<RoleDefinitionFilter>(f => f.Type == "BuiltInRole");
            return await authClient.RoleDefinitions.ListAsync(scope, roleFilter);
        }

        private async Task<List<Microsoft.Azure.Management.Authorization.Models.RoleAssignment>> GetRoleAssignmentsForCurrentUser(
            AuthorizationManagementClient authClient,
            string scope,
            List<Microsoft.Azure.Management.Authorization.Models.RoleDefinition> roleDefinitions)
        {
            var user = GetUser();
            var ownerObjectId = user.Claims.GetObjectId();
            return await GetRoleAssignmentsForUser(authClient, scope, ownerObjectId, roleDefinitions);
        }

        private async Task<List<Microsoft.Azure.Management.Authorization.Models.RoleAssignment>> GetRoleAssignmentsForUser(
            AuthorizationManagementClient authClient,
            string scope,
            string objectId,
            List<Microsoft.Azure.Management.Authorization.Models.RoleDefinition> roleDefinitions)
        {
            var filter = new ODataQuery<RoleAssignmentFilter>(f => f.PrincipalId == objectId);
            return await GetRoleAssignments(authClient, scope, roleDefinitions, filter);
        }

        private async Task<List<Microsoft.Azure.Management.Authorization.Models.RoleAssignment>> GetRoleAssignments(
            AuthorizationManagementClient authClient,
            string scope,
            List<Microsoft.Azure.Management.Authorization.Models.RoleDefinition> roleDefinitions,
            ODataQuery<RoleAssignmentFilter> filter = null)
        {
            var result = await authClient.RoleAssignments.ListForScopeAsync(scope, filter);
            var roleAssignments = result.ToList();
            foreach (var ra in roleAssignments)
            {
                Console.WriteLine($"Current Role Assignment: RoleName: {roleDefinitions.FirstOrDefault(rd => rd.Id == ra.RoleDefinitionId)?.RoleName}, Scope: {ra.Scope}");
            }
            return roleAssignments;
        }

        public static IDictionary<string, string> GetEnvironmentTags(string environmentName)
        {
            return new Dictionary<string, string> {{"Environment", environmentName ?? "Global"}};
        }

        private async Task RegisterProvider(Guid subscriptionId, string resourceProviderNamespace)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var rmClient = new ResourceManagementClient(token, _httpClientFactory.CreateClient(), false) { SubscriptionId = subscriptionId.ToString() };
            await rmClient.Providers.RegisterAsync(resourceProviderNamespace);
        }
    }
}
