// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ApplicationInsights.Management;
using Microsoft.Azure.Management.Authorization;
using Microsoft.Azure.Management.Batch;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;
using WebApp.Config;

namespace WebApp.Operations
{
    public sealed class ManagementClientMsiProvider : IManagementClientProvider
    {
        public async Task<IResourceManagementClient> CreateResourceManagementClient(Guid subscription)
        {
            var credentials = await GetCredentials();
            return new ResourceManagementClient(credentials) { SubscriptionId = subscription.ToString() };
        }

        public async Task<IBatchManagementClient> CreateBatchManagementClient(Guid subscription)
        {
            var credentials = await GetCredentials();
            return new BatchManagementClient(credentials) { SubscriptionId = subscription.ToString() };
        }

        public async Task<IComputeManagementClient> CreateComputeManagementClient(Guid subscription)
        {
            var credentials = await GetCredentials();
            return new ComputeManagementClient(credentials) { SubscriptionId = subscription.ToString() };
        }

        public async Task<IStorageManagementClient> CreateStorageManagementClient(Guid subscription)
        {
            var credentials = await GetCredentials();
            return new StorageManagementClient(credentials) { SubscriptionId = subscription.ToString() };
        }

        public async Task<IAuthorizationManagementClient> CreateAuthorizationManagementClient(Guid subscription)
        {
            var credentials = await GetCredentials();
            return new AuthorizationManagementClient(credentials) { SubscriptionId = subscription.ToString() };
        }

        public async Task<IApplicationInsightsManagementClient> CreateApplicationInsightsManagementClient(Guid subscription)
        {
            var credentials = await GetCredentials();
            return new ApplicationInsightsManagementClient(credentials) { SubscriptionId = subscription.ToString() };
        }

        public async Task<IKeyVaultManagementClient> CreateKeyVaultManagementClient(Guid subscription)
        {
            var credentials = await GetCredentials();
            return new KeyVaultManagementClient(credentials) { SubscriptionId = subscription.ToString() };
        }

        public async Task<INetworkManagementClient> CreateNetworkManagementClient(Guid subscription)
        {
            var credentials = await GetCredentials();
            return new NetworkManagementClient(credentials) { SubscriptionId = subscription.ToString() };
        }

        private async Task<ServiceClientCredentials> GetCredentials()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.core.windows.net/");
            return new TokenCredentials(accessToken);
        }
    }
}
