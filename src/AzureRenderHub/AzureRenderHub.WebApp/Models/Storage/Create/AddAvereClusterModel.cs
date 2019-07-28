// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
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
    }
}
