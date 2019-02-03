// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.ComponentModel.DataAnnotations;

using WebApp.Code;
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
            RepositoryName = cluster.Name;
            RepositoryType = cluster.RepositoryType;
            NewResourceGroupName = cluster.ResourceGroupName;

            if (cluster.SubscriptionId != null)
            {
                SubscriptionId = new Guid(cluster.SubscriptionId);
            }

            if (cluster.Subnet?.ResourceId != null)
            {
                SubnetResourceIdLocationAndAddressPrefix = cluster.Subnet.ToString();
            }
        }

        // TODO: rest of model stuff here

        [Required(ErrorMessage = Validation.Errors.Required.ResourceGroup)]
        [RegularExpression(Validation.RegularExpressions.ResourceGroup, ErrorMessage = Validation.Errors.Regex.ResourceGroup)]
        [StringLength(Validation.MaxLength.ResourceGroupName)]
        public string NewResourceGroupName { get; set; }
    }
}
