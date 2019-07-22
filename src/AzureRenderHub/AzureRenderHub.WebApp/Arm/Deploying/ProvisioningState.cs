using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureRenderHub.WebApp.Arm.Deploying
{
    public enum ProvisioningState
    {
        Unknown,
        Accepted,
        Running,
        Succeeded,
        Failed
    }
}
