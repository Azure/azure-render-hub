using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models.Environments.Create.Network
{
    public class VpnSettings
    {
        public VNetGatewaySettings VNetGatewaySettings { get; set; }

        public LocalNetworkGatewaySettings LocalNetworkGatewaySettings { get; set; }
    }
}
