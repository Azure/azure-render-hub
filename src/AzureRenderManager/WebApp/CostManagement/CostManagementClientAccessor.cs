// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using WebApp.Operations;

namespace WebApp.CostManagement
{
    public sealed class CostManagementClientAccessor : NeedsAccessToken
    {
        public CostManagementClientAccessor(IHttpContextAccessor contextAccessor, HttpClient httpClient)
            : base(contextAccessor)
        {
            _client = new Lazy<Task<CostManagementClient>>(async () => new CostManagementClient(httpClient, await GetAccessToken()));
        }

        private readonly Lazy<Task<CostManagementClient>> _client;

        public Task<CostManagementClient> GetClient() => _client.Value;
    }
}
