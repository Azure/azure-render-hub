// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WebApp.CostManagement
{
    public class CostManagementClient 
    {
        private readonly HttpClient _client;
        private readonly string _accessToken;

        public CostManagementClient(HttpClient client, string accessToken)
        {
            _client = client;
            _accessToken = accessToken;
        }

        private static Uri GetUri(string scope)
            => new Uri($"https://management.azure.com/{scope}/providers/Microsoft.CostManagement/query?api-version=2019-01-01");

        public async Task<UsageResponse> GetUsageForResourceGroup(Guid subscriptionId, string resourceGroupName, UsageRequest usageRequest)
        {
            var uri = GetUri($"subscriptions/{subscriptionId}/resourcegroups/{resourceGroupName}");

            var request =
                new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(usageRequest), Encoding.UTF8, "application/json"),
                };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            using (var response = await _client.SendAsync(request))
            {
                return await response.Content.ReadAsAsync<UsageResponse>();
            }
        }
    }
}
