// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace WebApp.Config.Resources
{
    public class GenericResource
    {
        public GenericResource(Microsoft.Azure.Management.ResourceManager.Models.GenericResource resourceInner)
        {
            if (resourceInner != null)
            {
                Id = resourceInner.Id;
                Name = resourceInner.Name;
                Location = resourceInner.Location;
                Type = resourceInner.Type;
            }
        }

        public string Id { get; }

        public string Name { get; }

        public string Location { get; }

        public string Type { get; }

        public bool Ignorable
        {
            get
            {
                // todo: pretty crude check for now to ignore batch created resources
                return Name == null || 
                    Name.Contains("-azurebatch-cloudserviceloadbalancer", StringComparison.OrdinalIgnoreCase) || 
                    Name.Contains("-azurebatch-cloudservicenetworksecuritygroup", StringComparison.OrdinalIgnoreCase) || 
                    Name.Contains("-azurebatch-cloudservicepublicip", StringComparison.OrdinalIgnoreCase);
            }
        }

        public string Icon
        {
            get
            {
                var lowerType = !string.IsNullOrEmpty(Type) ? Type.ToLower() : "";
                switch (lowerType)
                {
                    case "microsoft.keyvault/vaults":
                        return "key.svg";
                    case "microsoft.network/virtualnetworks":
                        return "vnet.svg";
                    case "microsoft.storage/storageaccounts":
                        return "storage.svg";
                    case "microsoft.insights/components":
                        return "insights.svg";
                    case "microsoft.insights/alertrules":
                        return "insights.svg";
                    case "microsoft.batch/batchaccounts":
                        return "batch.svg";
                    case "microsoft.network/loadbalancers":
                        return "load-balancer.svg";
                    case "microsoft.network/networksecuritygroups":
                        return "nsg.svg";
                    case "microsoft.network/publicipaddresses":
                        return "ip-address.svg";
                    default:
                        return "unknown.svg";
                }
            }
        }

        public string TypeDesc
        {
            get
            {
                var lowerType = !string.IsNullOrEmpty(Type) ? Type.ToLower() : ""; 
                switch (lowerType)
                {
                    case "microsoft.keyvault/vaults":
                        return "Key vault";
                    case "microsoft.network/virtualnetworks":
                        return "Virtual network";
                    case "microsoft.storage/storageaccounts":
                        return "Storage account";
                    case "microsoft.insights/components":
                        return "App Insights";
                    case "microsoft.insights/alertrules":
                        return "App Insights alert rules";
                    case "microsoft.batch/batchaccounts":
                        return "Batch account";
                    case "microsoft.network/loadbalancers":
                        return "Load balancer";
                    case "microsoft.network/networksecuritygroups":
                        return "Network security group";
                    case "microsoft.network/publicipaddresses":
                        return "Public IP address";
                    default:
                        return Type;
                }
            }
        }
    }
}
