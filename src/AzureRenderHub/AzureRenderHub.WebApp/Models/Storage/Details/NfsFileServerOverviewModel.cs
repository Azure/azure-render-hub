// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using AzureRenderHub.WebApp.Arm.Deploying;
using System.Collections.Generic;
using WebApp.Config.Storage;

namespace WebApp.Models.Storage.Details
{
    public class NfsFileServerOverviewModel : AssetRepositoryOverviewModel
    {
        public NfsFileServerOverviewModel()
        {
            // default constructor needed for model binding
        }

        public NfsFileServerOverviewModel(NfsFileServer fileServer)
        {
            if (fileServer != null)
            {
                Name = fileServer.Name;
                RepositoryType = fileServer.RepositoryType;
                SubscriptionId = fileServer.SubscriptionId;
                Username = fileServer.Username;
                Password = fileServer.Password;
                VmName = fileServer.VmName;
                PublicIp = fileServer.PublicIp;
                PrivateIp = fileServer.PrivateIp;
                VmSize = fileServer.VmSize;
                State = fileServer.State;
                ResourceGroupName = fileServer.ResourceGroupName;
                DeploymentName = fileServer.Deployment?.DeploymentName;
                DeploymentUrl = fileServer.Deployment?.DeploymentLink;
                FileShares = fileServer.FileShares;
                AllowedNetworks = fileServer.AllowedNetworks;

                if (fileServer.Subnet != null)
                {
                    SubnetName = fileServer.Subnet.Name;
                    SubnetVNetName = fileServer.Subnet.VNetName;
                    SubnetResourceId = fileServer.Subnet.ResourceId;
                    SubnetPrefix = fileServer.Subnet.AddressPrefix;
                    SubnetLocation = fileServer.Subnet.Location;
                }
            }
        }

        public List<NfsFileShare> FileShares { get; set; }

        // e.g. 10.2.0.0/24
        public List<string> AllowedNetworks { get; set; }

        // overrides
        public override string PowerStatus { get; set; }

        public override string DisplayName => "File Server";

        public override string Description => "Details about the File Server";
    }
}
