// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Authorization.Models;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Azure.Management.ResourceManager.Models;

using WebApp.Config;
using WebApp.Models.Environments;

namespace WebApp.Arm
{
    public interface IAzureResourceProvider
    {
        Task<ResourceGroup> CreateResourceGroupAsync(
            Guid subscriptionId,
            string location,
            string resourceGroupName,
            string environmentName);

        Task<List<GenericResource>> ListResourceGroupResources(
            string subscriptionId,
            string rgName);

        Task<bool> CanCreateResources(
            Guid subscriptionId);

        Task<bool> CanCreateRoleAssignments(
            Guid subscriptionId,
            string resourceGroupName);

        Task<List<ClassicAdministrator>> ListClassicAdministrators(Guid subscriptionId);

        Task<bool> IsCurrentUserClassicAdministrator(
            Guid subscriptionId);

        Task DeleteResourceGroupAsync(Guid subscriptionId, string resourceGroupName);

        Task<CheckNameAvailabilityResult> ValidateKeyVaultName(
            Guid subscriptionId, string resourceGroupName, string keyVaultName);

        Task<Vault> CreateKeyVaultAsync(
            Identity.Identity portalIdentity,
            Identity.Identity ownerIdentity,
            Guid subscriptionId,
            string resourceGroupName,
            string location,
            string keyVaultName,
            string environmentName);

        Task AddReaderIdentityToAccessPolicies(
            Guid subscriptionId,
            KeyVault keyVault,
            ServicePrincipal identity);

        Task DeleteKeyVaultAsync(
            Guid subscriptionId,
            KeyVault keyVault);

        Task<StorageAccount> CreateStorageAccountAsync(
            Guid subscriptionId,
            string location,
            string resourceGroupName,
            string storageAccountName,
            string environmentName);

        Task CreateFilesShare(Guid subscriptionId,
            string resourceGroupName,
            string storageAccountName,
            string filesShareName);

        Task<StorageProperties> GetStorageProperties(Guid subscriptionId,
            string resourceGroupName,
            string storageAccountName);

        Task DeleteStorageAccountAsync(Guid subscriptionId, string resourceGroupName, string storageAccountName);

        Task<BatchAccount> CreateBatchAccountAsync(
            Guid subscriptionId,
            string location,
            string resourceGroupName,
            string batchAccountName,
            string storageAccountResourceId,
            string environmentName);

        Task DeleteBatchAccountAsync(Guid subscriptionId, string resourceGroupName, string batchAccountName);

        Task UploadCertificateToBatchAccountAsync(
            Guid subscriptionId,
            BatchAccount batchAccount,
            X509Certificate2 certificate,
            string password);

        Task<ApplicationInsightsAccount> CreateApplicationInsightsAsync(
            Guid subscriptionId,
            string location,
            string resourceGroupName,
            string appInsightsName,
            string environmentName);

        Task<string> CreateApplicationInsightsApiKey(
            Guid subscriptionId,
            ApplicationInsightsAccount appInsights,
            string apiKeyName);

        Task DeleteApplicationInsightsAsync(Guid subscriptionId, string resourceGroupName, string appInsightsName);

        Task<Subnet> CreateVnetAsync(
            Guid subscriptionId,
            string location,
            string resourceGroupName,
            string vnetName,
            string subnetName,
            string vnetAddressSpace,
            string subnetAddressRange,
            string environmentName);

        Task<Subnet> GetVnetAsync(Guid subscriptionId, string location, string resourceGroupName, string vnetName, string subnetName);

        Task DeleteVNetAsync(Guid subscriptionId, string resourceGroupName, string vnetName);

        Task<List<UserPermission>> GetUserPermissions(Guid subscriptionId, string scope);

        Task AssignRoleToIdentityAsync(Guid subscriptionId, string resourceId, string role, Identity.Identity identity);
    }
}
