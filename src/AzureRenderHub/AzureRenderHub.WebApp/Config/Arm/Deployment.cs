using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Config.Arm
{
    public class Deployment
    {
        public Guid SubscriptionId { get; set; }

        public string ResourceGroupName { get; set; }

        public string ResourceGroupResourceId => $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}";

        public string DeploymentName { get; set; }

        public string DeploymentResourceId => $"https://ms.portal.azure.com/#blade/HubsExtension/DeploymentDetailsBlade/overview/id/%2Fsubscriptions%2F82acd5bb-4206-47d4-9c12-a65db028483d%2FresourceGroups%2FAvere-christis-3%2Fproviders%2FMicrosoft.Resources%2Fdeployments%2FAvere-dd55fa6e-184e-4130-acf0-1772c3af298d";

        public string DeploymentLink => $"https://ms.portal.azure.com/#blade/HubsExtension/DeploymentDetailsBlade/overview/id/%2Fsubscriptions%2F82acd5bb-4206-47d4-9c12-a65db028483d%2FresourceGroups%2FAvere-christis-3%2Fproviders%2FMicrosoft.Resources%2Fdeployments%2FAvere-dd55fa6e-184e-4130-acf0-1772c3af298d";

    }
}
