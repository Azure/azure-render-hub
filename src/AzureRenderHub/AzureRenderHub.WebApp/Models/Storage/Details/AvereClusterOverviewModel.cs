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

        public AvereClusterOverviewModel(AvereCluster avereCluster) : base (avereCluster)
        {
            if (avereCluster != null)
            {
                VServerIPRange = avereCluster.VServerIPRange;
                ManagementIP = avereCluster.ManagementIP;
                SshConnectionDetails = avereCluster.SshConnectionDetails;
            }
        }

        // overrides
        public override string DisplayName => "Avere Cluster";

        public override string Description => "Details about the Avere Cluster";

        public string VServerIPRange { get; set; }

        public string ManagementIP { get; set; }

        public string SshConnectionDetails { get; set; }
    }
}
