// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using WebApp.Code.Attributes;
using WebApp.Models.Storage.Create;

namespace WebApp.Config.Storage
{
    public class AvereCluster : AssetRepository
    {
        public AvereCluster()
        {
            RepositoryType = AssetRepositoryType.AvereCluster;
            ClusterName = "avere-cluster";
            ControllerName = "avere-ctrl";
            ControllerUserName = "avere";
            AvereBackedStorageAccountName = $"avere{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        public override void UpdateFromModel(AddAssetRepoBaseModel genericModel)
        {
            var model = genericModel as AddAvereClusterModel;
            if (model == null)
            {
                throw new ArgumentException("View model was not of type: AddNfsFileServerModel");
            }

            SubscriptionId = model.SubscriptionId.GetValueOrDefault();
            ResourceGroupName = model.NewResourceGroupName;
            Subnet = new Subnet(model.SubnetResourceIdLocationAndAddressPrefix);

            UseControllerPasswordCredential = model.UseControllerPasswordCredential;
            ControllerPasswordOrSshKey = model.UseControllerPasswordCredential 
                ? model.ControllerPassword 
                : model.ControllerSshKey;
            ManagementAdminPassword = model.AdminPassword;
            ClusterName = model.ClusterName;
            VmSize = model.VMSize;
            NodeCount = model.NodeCount;
            AvereCacheSizeGB = model.CacheSizeInGB;
        }

        public string ControllerName { get; set; }

        [JsonIgnore]
        public bool UseControllerPasswordCredential { get; set; }

        public string ControllerPasswordOrSshKey { get; set; }

        public string ControllerUserName { get; set; }

        [JsonIgnore]
        [Credential("AvereAdminPassword")]
        public string ManagementAdminPassword { get; set; }

        public string ClusterName { get; set; }

        public string VmSize { get; set; }

        public string AvereBackedStorageAccountName { get; set; }

        [JsonIgnore]
        public int NodeCount { get; set; }

        public int AvereCacheSizeGB { get; set; }

        public override Dictionary<string, object> GetTemplateParameters()
        {
            return new Dictionary<string, object>
            {
                {"environmentNameTag", EnvironmentName ?? "Global"},
                {"createVirtualNetwork", false},
                {"virtualNetworkResourceGroup", Subnet.ResourceGroupName},
                {"virtualNetworkName", Subnet.VNetName},
                {"virtualNetworkSubnetName", Subnet.Name},
                {"avereBackedStorageAccountName", AvereBackedStorageAccountName},
                {"controllerName", ControllerName},
                {"controllerAdminUsername", ControllerUserName},
                {"controllerAuthenticationType", UseControllerPasswordCredential ? "password" : "sshPublicKey"},
                {"controllerPassword", UseControllerPasswordCredential ? ControllerPasswordOrSshKey : string.Empty},
                {"controllerSSHKeyData", UseControllerPasswordCredential ? string.Empty : ControllerPasswordOrSshKey },
                {"adminPassword", ManagementAdminPassword},
                {"avereClusterName", ClusterName},
                {"avereInstanceType", VmSize},
                {"avereNodeCount", NodeCount},
                {"avereCacheSizeGB", AvereCacheSizeGB},
                {"rbacRoleAssignmentUniqueId", Guid.NewGuid().ToString()}
            };
        }

        public override string GetTemplateName()
        {
            return "Avere-vFXT.json";
        }
    }
}
