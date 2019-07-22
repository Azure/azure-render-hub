using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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

        [JsonConverter(typeof(StringEnumConverter))]
        public ProvisioningState ProvisioningState { get; set; }

        public string Error { get; set; }

        [JsonIgnore]
        public string DeploymentResourceId => DeploymentLink;

        [JsonIgnore]
        public string DeploymentLink => $"https://portal.azure.com/#blade/HubsExtension/DeploymentDetailsBlade/overview/id/%2Fsubscriptions%2F{SubscriptionId}%2FresourceGroups%2F{ResourceGroupName}%2Fproviders%2FMicrosoft.Resources%2Fdeployments%2F{DeploymentName}";
    }
}
