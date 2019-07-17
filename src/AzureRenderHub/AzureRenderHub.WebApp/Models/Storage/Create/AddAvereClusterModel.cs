// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.ComponentModel.DataAnnotations;

using WebApp.Code;
using WebApp.Config;
using WebApp.Config.Storage;

namespace WebApp.Models.Storage.Create
{
    public class AddAvereClusterModel : AddAssetRepoBaseModel
    {
        // needs this empty constructor for model bindings
        public AddAvereClusterModel()
        {
            RepositoryType = AssetRepositoryType.AvereCluster;
        }

        public AddAvereClusterModel(AvereCluster cluster)
        {
            SubscriptionId = cluster.SubscriptionId;
            RepositoryName = cluster.Name;
            RepositoryType = cluster.RepositoryType;
            NewResourceGroupName = cluster.ResourceGroupName;
            ClusterName = cluster.ClusterName;
            VMSize = cluster.VmSize;
            NodeCount = cluster.NodeCount;
            CacheSizeInGB = cluster.AvereCacheSizeGB;
            UseControllerPasswordCredential = cluster.UseControllerPasswordCredential;
            ControllerPassword = cluster.UseControllerPasswordCredential ? cluster.ControllerPasswordOrSshKey : null;
            ControllerSshKey = cluster.UseControllerPasswordCredential ? null : cluster.ControllerPasswordOrSshKey;
            AdminPassword = cluster.ManagementAdminPassword;
            Subnet = cluster.Subnet;
            ExistingVNetName = cluster.Subnet?.VNetName;
            ExistingSubnetName = $"{cluster.Subnet?.VNetName} - {cluster.Subnet?.Name} ({cluster.Subnet?.AddressPrefix})";
            ExistingSubnetAddressPrefix = cluster.Subnet?.AddressPrefix;
            VNetAddressSpace = cluster.Subnet?.VNetAddressPrefixes;
            CreateSubnet = true;
        }

        [Required(ErrorMessage = Validation.Errors.Required.ResourceGroup)]
        [RegularExpression(Validation.RegularExpressions.ResourceGroup, ErrorMessage = Validation.Errors.Regex.ResourceGroup)]
        [StringLength(Validation.MaxLength.ResourceGroupName)]
        public string NewResourceGroupName { get; set; }

        public string ClusterName { get; set; } = "avere-cluster";

        public bool UseControllerPasswordCredential { get; set; } = true;

        [Display(Name = "Controller Password", Description = "The Avere controller node password.")]
        public string ControllerPassword { get; set; }

        [Display(Name = "Controller SSH Key", Description = "The Avere controller node SSH key.")]
        public string ControllerSshKey { get; set; }

        [Required]
        [MinLength(8)]
        [MaxLength(64)]
        [Display(Name = "Admin Password", Description = "The Avere management administrator password.")]
        public string AdminPassword { get; set; }

        [Required]
        [Display(Name = "Virtual Machine Size", Description = "The VM size must be either Standard_E8s_v3, Standard_E16s_v3 or Standard_E32s_v3.")]
        public string VMSize { get; set; }

        [Required]
        [Range(3, 12)]
        [Display(Name = "Avere Node Count", Description = "The number of Avere vFXT nodes in the cluster.")]
        public int NodeCount { get; set; }

        [Required]
        [Range(1024, 4096)]
        [Display(Name = "Avere Cache Size in GB", Description = "The cache size in GB to use for each Avere vFXT VM.")]
        public int CacheSizeInGB { get; set; }

        public bool CreateSubnet { get; set; }

        public string ExistingVNetName { get; set; }

        public string ExistingSubnetName { get; set; }

        public string ExistingSubnetAddressPrefix { get; set; }

        public string VNetAddressSpace { get; set; }

        [RegularExpression(Validation.RegularExpressions.ResourceGroup, ErrorMessage = Validation.Errors.Regex.Subnet)]
        [StringLength(Validation.MaxLength.ResourceGroupName)]
        public string NewSubnetName { get; set; }

        // e.g. CIDR 10.2.0.0/24
        [RegularExpression(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\/(3[0-2]|[1-2][0-9]|[0-9]))$",
            ErrorMessage = Validation.Errors.Regex.SubnetAddressRange)]
        public string NewSubnetAddressPrefix { get; set; }
    }
}
