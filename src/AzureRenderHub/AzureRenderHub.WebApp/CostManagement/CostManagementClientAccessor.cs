// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using WebApp.Operations;

namespace WebApp.CostManagement
{
    public sealed class CostManagementClientAccessor : NeedsAccessToken
    {
        public CostManagementClientAccessor(
            IHttpContextAccessor contextAccessor,
            ITokenAcquisition tokenAcquisition,
            HttpClient httpClient)
            : base(contextAccessor, tokenAcquisition)
        {
            _client = new Lazy<Task<CostManagementClient>>(async () => new CostManagementClient(httpClient, await GetAccessToken()));
        }

        private readonly Lazy<Task<CostManagementClient>> _client;

        public Task<CostManagementClient> GetClient() => _client.Value;
    }
}
