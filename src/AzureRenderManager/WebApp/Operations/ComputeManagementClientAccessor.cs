// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Management.Compute;
using Microsoft.Rest;
using WebApp.Config;

namespace WebApp.Operations
{
    public sealed class ComputeManagementClientAccessor : NeedsAccessToken, IDisposable
    {
        private readonly PortalConfigurationAccessor _configAccessor;
        private readonly ConcurrentDictionary<string, Task<IComputeManagementClient>> _cache = new ConcurrentDictionary<string, Task<IComputeManagementClient>>();

        public ComputeManagementClientAccessor(IHttpContextAccessor accessor, PortalConfigurationAccessor configAccessor)
            : base(accessor)
        {
            _configAccessor = configAccessor;
        }

        private async Task<IComputeManagementClient> CreateComputeManagementClient(string subscriptionId, PortalConfigurationAccessor configAccessor)
        {
            var accessToken = await GetAccessToken();

            var tokenCredentials = new TokenCredentials(accessToken);
            return new ComputeManagementClient(tokenCredentials) { SubscriptionId = subscriptionId };
        }

        public Task<IComputeManagementClient> ForSubscription(string subscriptionId)
            => _cache.GetOrAdd(subscriptionId, CreateComputeManagementClient, _configAccessor);

        public void Dispose()
        {
            foreach (var value in _cache.Values)
            {
                if (value.IsCompletedSuccessfully)
                {
                    value.Result.Dispose();
                }
            }
        }
    }
}