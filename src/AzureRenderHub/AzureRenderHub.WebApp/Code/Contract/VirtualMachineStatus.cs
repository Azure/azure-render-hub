using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureRenderHub.WebApp.Code.Contract
{
    public class VirtualMachineStatus
    {
        public Guid SubscriptionId { get; set; }

        public string ResourceGroupName { get; set; }

        public string VirtualMachineName { get; set; }

        public string PowerStatus { get; set; }

        public bool IsRunning()
        {
            return PowerStatus != null && PowerStatus.Equals("VM running");
        }

        public string PortalLink => 
            $"https://portal.azure.com/#resource/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.Compute/virtualMachines/{VirtualMachineName}";
    }
}
