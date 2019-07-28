// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AzureRenderHub.WebApp.Arm.Deploying;
using AzureRenderHub.WebApp.Code.Contract;
using AzureRenderHub.WebApp.Config.Storage;
using Microsoft.Azure.Management.Compute.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApp.Config.Storage;

namespace WebApp.Models.Storage.Details
{
    public abstract class AssetRepositoryOverviewModel
    {
        public AssetRepositoryOverviewModel(AssetRepository storage = null)
        {
            if (storage != null)
            {
                Name = storage.Name;
                RepositoryType = storage.RepositoryType;
                SubscriptionId = storage.SubscriptionId;
                ResourceGroupName = storage.ResourceGroupName;
                State = storage.State;

                if (storage.Subnet != null)
                {
                    SubnetName = storage.Subnet.Name;
                    SubnetVNetName = storage.Subnet.VNetName;
                    SubnetResourceId = storage.Subnet.ResourceId;
                    SubnetPrefix = storage.Subnet.AddressPrefix;
                    SubnetLocation = storage.Subnet.Location;
                }

                if (storage.Deployment != null)
                {
                    DeploymentName = storage.Deployment.DeploymentName;
                    DeploymentUrl = storage.Deployment.DeploymentLink;
                    DeploymentState = storage.Deployment.ProvisioningState;
                }
            }
        }

        public string Name { get; set; }

        public AssetRepositoryType RepositoryType { get; protected set; }

        public Guid SubscriptionId { get; set; }

        public string SubnetName { get; set; }

        public string SubnetVNetName { get; set; }

        public string SubnetResourceId { get; set; }

        public string SubnetLocation { get; set; }

        public string SubnetPrefix { get; set; }

        public int VmCores { get; set; }

        public int VmMemory { get; set; }

        public string ResourceGroupName { get; set; }

        public string ResourceGroupUrl =>
            $"https://portal.azure.com/#resource/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}";

        public string DeploymentName { get; set; }

        public string DeploymentUrl { get; set; }

        public ProvisioningState DeploymentState { get; set; }

        public StorageState State { get; set; }

        public IEnumerable<VirtualMachineStatus> VirtualMachineStatus { get; set; }

        public bool IsRunning()
        {
            return VirtualMachineStatus != null && VirtualMachineStatus.All(vms => vms.IsRunning());
        }

        public abstract string DisplayName { get; }

        public abstract string Description { get; }
    }
}
