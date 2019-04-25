using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models.Environments.Create.Network
{
    public class VNetSettings
    {
        [Required]
        public string VNetName { get; set; } = "RenderingSubnet";

        // e.g. 10.1.0.0/16
        [Required]
        public string VNetAddressSpace { get; set; } = "10.24.0.0/16";

        [Required]
        public string SubnetName { get; set; } = "RenderNodeSubnet";

        // e.g. 10.2.0.0/24 (10.1.0.0-10.1.0.255)
        // The subnet range must be within the VNet space
        [Required]
        public string SubnetAddressRange { get; set; } = "10.24.0.0/24";

        public string DnsServer { get; set; }

        // Required for VPN
        public string GatewaySubnetAddressRange { get; set; } = "10.24.1.0/27";
    }
}
