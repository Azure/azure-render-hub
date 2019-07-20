using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureRenderHub.WebApp.Arm.Deploying
{
    public class SubnetDeployment : IDeployable
    {
        public SubnetDeployment(
            Deployment deploymentSpec,
            string existingVirtualNetworkName,
            string newSubnetName,
            string newSubnetAddressPrefix)
        {
            Deployment = deploymentSpec;
            VirtualNetworkName = existingVirtualNetworkName;
            SubnetName = newSubnetName;
            SubnetAddressPrefix = newSubnetAddressPrefix;
        }

        public Deployment Deployment { get; }

        public string VirtualNetworkName { get; }

        public string SubnetName { get; }

        public string SubnetAddressPrefix { get; }

        public Guid SubscriptionId => Deployment.SubscriptionId;

        public string Location => Deployment.Location;

        public string ResourceGroupName => Deployment.ResourceGroupName;

        public string DeploymentName => Deployment.DeploymentName;

        public string TemplateName => "AddSubnet.json";

        public Dictionary<string, object> DeploymentParameters
        {
            get
            {
                return new Dictionary<string, object>
                {
                    {"virtualNetworkName", VirtualNetworkName},
                    {"virtualNetworkSubnetName", SubnetName},
                    {"subnetAddressRangePrefix", SubnetAddressPrefix},
                };
            }
        }
    }
}