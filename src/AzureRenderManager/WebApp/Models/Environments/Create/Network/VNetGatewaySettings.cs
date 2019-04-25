using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models.Environments.Create.Network
{
    public class VNetGatewaySettings
    {
        public string GatewayName { get; set; } = "RenderingVNetGateway";

        public string Sku { get; set; } = "VpnGw1";

        // The Shared Key configured in your on-prem VPN device
        public string ConnectionSharedKey { get; set; } = "VpnGw2";
    }
}
