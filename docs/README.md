# Documentation and Guides

These docs will guide you through the setup and configuration process of the Render Farm Manager.  

# Prerequisites

**Render Farm**

You will need an existing Qube supervisor or Deadline 10 deployment, on-prem or in Azure.  If you're
connecting Azure VMs to an on-prem render farm you will need an existing Express Route or VPN connection
and associated Azure VNet.

**Connection to Azure (for Hybrid)**

Site-to-site VPN connections to Azure are reasonably straightforward to deploy and setup.  For more information
checkout the setup guide [here](https://docs.microsoft.com/en-us/azure/vpn-gateway/vpn-gateway-howto-site-to-site-resource-manager-portal).

# Setup Steps

1. [Deploy the Render Farm Manager Portal](00-deployment.md)

2. [Create an Environment](10-environments-overview.md)

3. [Create a Package](20-packages-overview.md) (Optional)

4. [Create Avere vFXT or File Server](30-storage-overview.md) (Optional)

5. [Create a Custom Image](40-customimages-overview.md)