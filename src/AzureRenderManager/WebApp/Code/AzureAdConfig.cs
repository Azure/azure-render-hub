using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Code
{
    public class AzureAdConfig
    {
        public string AADInstance { get; set; }
        public string CallbackPath { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Domain { get; set; }
        public string TenantId { get; set; }
        public string GraphResourceId { get; set; }
        public string AuthEndpointPrefix { get; set; }
    }
}
