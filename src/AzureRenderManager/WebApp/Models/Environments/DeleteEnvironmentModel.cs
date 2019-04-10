// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using WebApp.Code.Validators;
using WebApp.Config;
using WebApp.Config.Resources;

namespace WebApp.Models.Environments
{
    public class DeleteEnvironmentModel : EnvironmentBaseModel
    {
        // needs this empty constructor for model bindings
        public DeleteEnvironmentModel()
        {
            Resources = new List<GenericResource>();
        }

        public DeleteEnvironmentModel(RenderingEnvironment environment, List<GenericResource> resources = null)
        {
            // default to deleting the resource group
            // TODO: add a flag to the env to verify if customer or us created the RG. 
            Resources = resources ?? new List<GenericResource>();

            if (environment != null)
            {
                EnvironmentName = environment.Name;
                SubscriptionId = environment.SubscriptionId;
                LocationName = environment.LocationName;
                ResourceGroup = environment.ResourceGroupName;
                DeleteResourceGroup = !string.IsNullOrEmpty(ResourceGroup);
                BatchAccount = environment.BatchAccount?.Name;
                StorageAccount = environment.StorageAccount?.Name;
                ApplicationInsights = environment.ApplicationInsightsAccount?.Name;
                KeyVault = environment.KeyVault?.Name;
                VNet = environment.Subnet?.VNetName;
            }
        }

        // Details

        public Guid? SubscriptionId { get; set; }

        public string LocationName { get; set; }

        public string ResourceGroup { get; set; }

        public bool HasResourceGroup => false == string.IsNullOrEmpty(ResourceGroup);

        [Required(ErrorMessage = "Please enter the environment name to confirm delete.")]
        [ConfirmDeletion(ErrorMessage = "The entered name must match the current environment name.")]
        public string Confirmation { get; set; }

        public List<GenericResource> Resources { get; }

        public int ResourceCount => Resources.Count;

        public bool ResourceLoadFailed { get; set; }

        public string BatchAccount { get; set; }

        public string StorageAccount { get; set; }

        public string ApplicationInsights { get; set; }

        public string KeyVault { get; set; }

        public string VNet { get; set; }

        public bool HasAnyResources => 
            BatchAccount != null || 
            StorageAccount != null || 
            ApplicationInsights != null || 
            KeyVault != null || 
            VNet != null;

        // Delete Options

        public bool DeleteResourceGroup { get; set; }

        public bool DeleteBatchAccount { get; set; }

        public bool DeleteStorageAccount { get; set; }

        public bool DeleteAppInsights { get; set; }

        public bool DeleteKeyVault { get; set; }

        public bool DeleteVNet { get; set; }
    }
}
