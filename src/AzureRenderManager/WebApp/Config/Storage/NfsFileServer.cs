// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;

using WebApp.Models.Storage.Create;

namespace WebApp.Config.Storage
{
    public class NfsFileServer : AssetRepository
    {
        public NfsFileServer()
        {
            RepositoryType = AssetRepositoryType.NfsFileServer;
        }

        public string Username { get; set; }

        public string VmName { get; set; }

        public string PublicIp { get; set; }

        public string PrivateIp { get; set; }

        public string VmSize { get; set; }

        public List<NfsFileShare> FileShares { get; set; }

        // e.g. 10.2.0.0/24
        public List<string> AllowedNetworks { get; set; }

        public override void UpdateFromModel(AddAssetRepoBaseModel addModel)
        {
            var nfsModel = addModel as AddNfsFileServerModel;
            if (nfsModel == null)
            {
                throw new ArgumentException("View model was not of type: AddNfsFileServerModel");
            }

            Name = nfsModel.RepositoryName;
            SubscriptionId = nfsModel.SubscriptionId.Value;
            ResourceGroupName = nfsModel.NewResourceGroupName;
            Subnet = new Subnet
            {
                ResourceId = nfsModel.SubnetResourceIdLocationAndAddressPrefix.Split(";")[0],
                Location = nfsModel.SubnetResourceIdLocationAndAddressPrefix.Split(";")[1],
                AddressPrefix = nfsModel.SubnetResourceIdLocationAndAddressPrefix.Split(";")[2],
            };

            VmSize = nfsModel.VmSize;
            VmName = nfsModel.VmName;
            Username = nfsModel.UserName;

            AllowedNetworks = nfsModel.AllowedNetworks?.Split(",").ToList();

            // TODO: allow for multiple of these
            FileShares = new List<NfsFileShare>(new[]
            {
                new NfsFileShare
                {
                    Name = nfsModel.FileShareName,
                    Type = nfsModel.FileShareType,
                }
            });
        }
    }
}
