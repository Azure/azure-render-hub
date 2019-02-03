// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WebApp.AppInsights
{
    public class AppInsightsQueryProvider : IAppInsightsQueryProvider
    {
        private const string QueryUrl = "https://api.applicationinsights.io/v1/apps/{0}/query?query={1}";

        public async Task<AppInsightsQueryResult> ExecuteQuery(string applicationId, string apiKey, string query)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            client.DefaultRequestHeaders.Add("x-api-key", apiKey);

            var req = string.Format(
                QueryUrl,
                applicationId,
                Uri.EscapeDataString(query));

            HttpResponseMessage response = await client.GetAsync(req);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return new AppInsightsQueryResult
                {
                    Success = true,
                    Results = JObject.Parse(result),
                };
            }

            return new AppInsightsQueryResult
            {
                Success = false,
                Error = response.ReasonPhrase,
            };
        }
    }
}
