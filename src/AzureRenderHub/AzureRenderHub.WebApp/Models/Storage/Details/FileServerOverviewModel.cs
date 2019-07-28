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

        public NfsFileServerOverviewModel(NfsFileServer fileServer) : base(fileServer)
        {
            if (fileServer != null)
            {
                Username = fileServer.Username;
                Password = fileServer.Password;
                VmName = fileServer.VmName;
                VmSize = fileServer.VmSize;
                PublicIp = fileServer.PublicIp;
                PrivateIp = fileServer.PrivateIp;
                FileShares = fileServer.FileShares;
                AllowedNetworks = fileServer.AllowedNetworks;
            }
        }

        public List<NfsFileShare> FileShares { get; set; }

        // e.g. 10.2.0.0/24
        public List<string> AllowedNetworks { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string VmName { get; set; }

        public string PublicIp { get; set; }

        public string PrivateIp { get; set; }

        public string VmSize { get; set; }

        // overrides
        public override string DisplayName => "File Server";

        public override string Description => "Details about the File Server";
    }
}
