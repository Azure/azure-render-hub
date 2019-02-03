// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using WebApp.Config.Storage;

namespace WebApp.Models.Storage.Details
{
    public class AvereClusterDetailsModel : AssetRepositoryDetailsModel
    {
        public AvereClusterDetailsModel()
        {
            // default constructor needed for model binding
        }

        public AvereClusterDetailsModel(AvereCluster avereCluster)
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

        public override string Status { get; set; }

        public override string DisplayName => "Avere Cluster";

        public override string Description => "Details about the Avere Cluster";
    }
}
