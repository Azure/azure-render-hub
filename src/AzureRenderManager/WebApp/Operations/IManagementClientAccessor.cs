// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ApplicationInsights.Management;
using Microsoft.Azure.Management.Authorization;
using Microsoft.Azure.Management.Batch;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.Storage;
using WebApp.Config;

namespace WebApp.Operations
{
    public interface IManagementClientProvider
    {
        Task<IResourceManagementClient> CreateResourceManagementClient(Guid subscription);

        Task<IBatchManagementClient> CreateBatchManagementClient(Guid subscription);

        Task<IComputeManagementClient> CreateComputeManagementClient(Guid subscription);

        Task<IStorageManagementClient> CreateStorageManagementClient(Guid subscription);

        Task<IAuthorizationManagementClient> CreateAuthorizationManagementClient(Guid subscription);

        Task<IApplicationInsightsManagementClient> CreateApplicationInsightsManagementClient(Guid subscription);

        Task<IKeyVaultManagementClient> CreateKeyVaultManagementClient(Guid subscription);

        Task<INetworkManagementClient> CreateNetworkManagementClient(Guid subscription);
    }
}
