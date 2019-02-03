// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using WebApp.Config.Storage;

namespace WebApp.Models.Storage.Details
{
    public class NfsFileServerDetailsModel : AssetRepositoryDetailsModel
    {
        public NfsFileServerDetailsModel()
        {
            // default constructor needed for model binding
        }

        public NfsFileServerDetailsModel(NfsFileServer fileServer)
        {
            if (fileServer != null)
            {
                Name = fileServer.Name;
                RepositoryType = fileServer.RepositoryType;
                SubscriptionId = fileServer.SubscriptionId;
                Username = fileServer.Username;
                VmName = fileServer.VmName;
                PublicIp = fileServer.PublicIp;
                PrivateIp = fileServer.PrivateIp;
                VmSize = fileServer.VmSize;
                ProvisioningState = fileServer.ProvisioningState;
                ResourceGroupName = fileServer.ResourceGroupName;
                DeploymentName = fileServer.DeploymentName;
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

        public string Username { get; set; }

        public string VmName { get; set; }

        public string PublicIp { get; set; }

        public string PrivateIp { get; set; }

        public string VmSize { get; set; }

        public string ProvisioningState { get; set; }

        public string ResourceGroupName { get; set; }

        public string DeploymentName { get; set; }

        public List<NfsFileShare> FileShares { get; set; }

        // e.g. 10.2.0.0/24
        public List<string> AllowedNetworks { get; set; }

        // overrides

        public override string Status { get; set; }

        public override string DisplayName => "NFS File Server";

        public override string Description => "Details about the NFS File Server";
    }
}
