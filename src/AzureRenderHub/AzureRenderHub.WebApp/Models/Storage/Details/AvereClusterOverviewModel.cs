// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using AzureRenderHub.WebApp.Arm.Deploying;
using System.Collections.Generic;
using WebApp.Config.Storage;

namespace WebApp.Models.Storage.Details
{
    public class AvereClusterOverviewModel : AssetRepositoryOverviewModel
    {
        public AvereClusterOverviewModel()
        {
            // default constructor needed for model binding
        }

        public AvereClusterOverviewModel(AvereCluster avereCluster)
        {
            if (avereCluster != null)
            {
                Name = avereCluster.Name;
                RepositoryType = avereCluster.RepositoryType;
                SubscriptionId = avereCluster.SubscriptionId;
                VServerIPRange = avereCluster.VServerIPRange;
                ManagementIP = avereCluster.ManagementIP;
                SshConnectionDetails = avereCluster.SshConnectionDetails;

                if (avereCluster.Subnet != null)
                {
                    SubnetName = avereCluster.Subnet.Name;
                    SubnetVNetName = avereCluster.Subnet.VNetName;
                    SubnetResourceId = avereCluster.Subnet.ResourceId;
                    SubnetPrefix = avereCluster.Subnet.AddressPrefix;
                    SubnetLocation = avereCluster.Subnet.Location;
                }
            }
        }

        public ProvisioningState DeploymentState { get; set; }

        // overrides

        public override string PowerStatus { get; set; }

        public override string DisplayName => "Avere Cluster";

        public override string Description => "Details about the Avere Cluster";

        public string VServerIPRange { get; set; }

        public string ManagementIP { get; set; }

        public string SshConnectionDetails { get; set; }
    }
}
