using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Authorization.Models;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Azure.Management.ResourceManager.Models;
using WebApp.Arm;
using WebApp.Config;
using WebApp.Identity;
using WebApp.Models.Api;
using WebApp.Models.Environments;

namespace WebApp.Tests.Fakes
{
    class FakeAzureResourceProvider : IAzureResourceProvider
    {
        public FakeAzureResourceProvider()
        {
        }

        public Task AddReaderIdentityToAccessPolicies(Guid subscriptionId, KeyVault keyVault, ServicePrincipal identity)
        {
            throw new NotImplementedException();
        }

        public Task AssignRoleToIdentityAsync(Guid subscriptionId, string resourceId, string role, Identity.Identity identity)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CanCreateResources(Guid subscriptionId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CanCreateRoleAssignments(Guid subscriptionId, string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreateApplicationInsightsApiKey(Guid subscriptionId, ApplicationInsightsAccount appInsights, string apiKeyName)
        {
            throw new NotImplementedException();
        }

        public Task<ApplicationInsightsAccount> CreateApplicationInsightsAsync(Guid subscriptionId, string location, string resourceGroupName, string appInsightsName, string environmentName)
        {
            throw new NotImplementedException();
        }

        public Task<BatchAccount> CreateBatchAccountAsync(Guid subscriptionId, string location, string resourceGroupName, string batchAccountName, string storageAccountResourceId, string environmentName)
        {
            throw new NotImplementedException();
        }

        public Task CreateFilesShare(Guid subscriptionId, string resourceGroupName, string storageAccountName, string filesShareName)
        {
            throw new NotImplementedException();
        }

        public Task<Vault> CreateKeyVaultAsync(Identity.Identity portalIdentity, Identity.Identity ownerIdentity, Guid subscriptionId, string resourceGroupName, string location, string keyVaultName, string environmentName)
        {
            throw new NotImplementedException();
        }

        public Task<ResourceGroup> CreateResourceGroupAsync(Guid subscriptionId, string location, string resourceGroupName, string environmentName)
        {
            throw new NotImplementedException();
        }

        public Task<StorageAccount> CreateStorageAccountAsync(Guid subscriptionId, string location, string resourceGroupName, string storageAccountName, string environmentName)
        {
            throw new NotImplementedException();
        }

        public Task<Subnet> CreateVnetAsync(Guid subscriptionId, string location, string resourceGroupName, string vnetName, string subnetName, string vnetAddressSpace, string subnetAddressRange, string environmentName)
        {
            throw new NotImplementedException();
        }

        public Task DeleteApplicationInsightsAsync(Guid subscriptionId, string resourceGroupName, string appInsightsName)
        {
            throw new NotImplementedException();
        }

        public Task DeleteBatchAccountAsync(Guid subscriptionId, string resourceGroupName, string batchAccountName)
        {
            throw new NotImplementedException();
        }

        public Task DeleteKeyVaultAsync(Guid subscriptionId, KeyVault keyVault)
        {
            throw new NotImplementedException();
        }

        public Task DeleteResourceGroupAsync(Guid subscriptionId, string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public Task DeleteStorageAccountAsync(Guid subscriptionId, string resourceGroupName, string storageAccountName)
        {
            throw new NotImplementedException();
        }

        public Task DeleteVNetAsync(Guid subscriptionId, string resourceGroupName, string vnetName)
        {
            throw new NotImplementedException();
        }

        public Task<StorageProperties> GetStorageProperties(Guid subscriptionId, string resourceGroupName, string storageAccountName)
        {
            throw new NotImplementedException();
        }

        public List<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

        public Task<List<UserPermission>> GetUserPermissions(Guid subscriptionId, string scope)
        {
            // Return explicit and inherited scoped permissions
            return Task.FromResult(UserPermissions.Where(p => p.Scope == scope || scope.Contains(p.Scope)).ToList());
        }

        public Task<Subnet> GetSubnetAsync(Guid subscriptionId, string location, string resourceGroupName, string vnetName, string subnetName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsCurrentUserClassicAdministrator(Guid subscriptionId)
        {
            throw new NotImplementedException();
        }

        public List<ClassicAdministrator> ClassicAdministrators { get; set; } = new List<ClassicAdministrator>();

        public Task<List<ClassicAdministrator>> ListClassicAdministrators(Guid subscriptionId)
        {
            return Task.FromResult(ClassicAdministrators);
        }

        public Task<List<GenericResource>> ListResourceGroupResources(string subscriptionId, string rgName)
        {
            throw new NotImplementedException();
        }

        public Task UploadCertificateToBatchAccountAsync(Guid subscriptionId, BatchAccount batchAccount, X509Certificate2 certificate, string password)
        {
            throw new NotImplementedException();
        }

        public Task<CheckNameAvailabilityResult> ValidateKeyVaultName(Guid subscriptionId, string resourceGroupName, string keyVaultName)
        {
            throw new NotImplementedException();
        }

        public Task<Subnet> CreateSubnetAsync(Guid subscriptionId, string location, string resourceGroupName, string vnetName, string subnetName, string subnetAddressRange, string environmentName)
        {
            throw new NotImplementedException();
        }

        public Task CreateSubnetServiceEndpointAsync(Guid subscriptionId, string location, string resourceGroupName, string vnetName, string subnetName, string service)
        {
            throw new NotImplementedException();
        }
    }
}
