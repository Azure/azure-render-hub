// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Linq;
using Newtonsoft.Json;
using WebApp.Code.Attributes;

namespace WebApp.Config
{
    public class ApplicationInsightsAccount : AzureResource
    {
        public string ApplicationId { get; set; }

        public string InstrumentationKey { get; set; }

        [Credential("ApplicationInsightsApiKey")]
        [JsonIgnore]
        public string ApiKey { get; set; }
    }
}
