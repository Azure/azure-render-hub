// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
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

        // overrides

        public override ProvisioningState ProvisioningState { get; set; }

        public override string PowerStatus { get; set; }

        public override string DisplayName => "Avere Cluster";

        public override string Description => "Details about the Avere Cluster";

        public string VServerIPs { get; set; }

        public string ManagementIP { get; set; }

        public string SSHConnectionDetails { get; set; }
    }
}
