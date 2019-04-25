using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models.Environments.Create.Network
{
    public class LocalNetworkGatewaySettings
    {
        // Friendly name of your on-prem network/site
        public string SiteName { get; set; }

        // Public IP address of your VPN device or server
        public string IpAddress { get; set; }
    }
}
