// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace WebApp.Code.Extensions
{
    public class AzureAdOptions
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Instance { get; set; }

        public string Domain { get; set; }

        public string TenantId { get; set; }

        public string CallbackPath { get; set; }

        public string AADInstance { get; set; }

        public string GraphResourceId { get; set; }

        public string AuthEndpointPrefix { get; set; }

        public string GraphScopes { get; set; }

        public string Authority {
            get
            {
                return $"{Instance}{TenantId}";
            }
        }
    }
}
