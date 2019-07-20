using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureRenderHub.WebApp.Arm.Deploying
{
    public class Deployment
    {
        public Deployment(Guid subscriptionId, string location, string resourceGroupName, string deploymentName)
        {
            SubscriptionId = subscriptionId;
            Location = location;
            ResourceGroupName = resourceGroupName;
            DeploymentName = deploymentName;
        }

        public Deployment()
        {
        }

        public Guid SubscriptionId { get; set; }

        public string Location { get; set; }

        public string ResourceGroupName { get; set; }

        public string DeploymentName { get; set; }

        public ProvisioningState ProvisioningState { get; set; }

        public string Error { get; set; }

        public string DeploymentResourceId => DeploymentLink;

        public string DeploymentLink => $"https://portal.azure.com/#blade/HubsExtension/DeploymentDetailsBlade/overview/id/%2Fsubscriptions%2F82acd5bb-4206-47d4-9c12-a65db028483d%2FresourceGroups%2FAvere-christis-3%2Fproviders%2FMicrosoft.Resources%2Fdeployments%2FAvere-dd55fa6e-184e-4130-acf0-1772c3af298d";
    }
}
