using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureRenderHub.WebApp.Arm.Deploying
{
    public interface IDeployable
    {
        Guid SubscriptionId { get; }

        string Location { get; }

        string ResourceGroupName { get; }

        string TemplateName { get; }

        string DeploymentName { get; }

        Dictionary<string, object> DeploymentParameters { get; }
    }
}
