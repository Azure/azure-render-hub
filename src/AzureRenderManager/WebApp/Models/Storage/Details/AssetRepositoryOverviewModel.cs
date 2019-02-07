// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using WebApp.Config.Storage;

namespace WebApp.Models.Storage.Details
{
    public abstract class AssetRepositoryOverviewModel
    {
        public string Name { get; set; }

        public AssetRepositoryType RepositoryType { get; protected set; }

        public string SubscriptionId { get; set; }

        public string SubnetName { get; set; }

        public string SubnetVNetName { get; set; }

        public string SubnetResourceId { get; set; }

        public string SubnetLocation { get; set; }

        public string SubnetPrefix { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string VmName { get; set; }

        public string PublicIp { get; set; }

        public string PrivateIp { get; set; }

        public string VmSize { get; set; }

        public int VmCores { get; set; }

        public int VmMemory { get; set; }

        public string ResourceGroupName { get; set; }

        public string ResourceGroupUrl =>
            $"https://portal.azure.com/#resource/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}";

        public string DeploymentName { get; set; }

        public string DeploymentUrl =>
            $"https://portal.azure.com/#resource/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/deployments";

        public abstract ProvisioningState ProvisioningState { get; set; }

        public abstract string PowerStatus { get; set; }

        public abstract string DisplayName { get; }

        public abstract string Description { get; }
    }
}
