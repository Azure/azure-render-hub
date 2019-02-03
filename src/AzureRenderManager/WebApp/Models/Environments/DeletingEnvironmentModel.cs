// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using WebApp.Config;

namespace WebApp.Models.Environments
{
    public class DeletingEnvironmentModel : EnvironmentBaseModel
    {
        // needs this empty constructor for model bindings
        public DeletingEnvironmentModel()
        { }

        public DeletingEnvironmentModel(RenderingEnvironment environment)
        {
            if (environment != null)
            {
                EnvironmentName = environment.Name;
                SubscriptionId = environment.SubscriptionId;
                LocationName = environment.LocationName;
                ResourceGroup = environment.ResourceGroupName;
                BatchAccount = environment.BatchAccount?.Url;
                StorageAccount = environment.StorageAccount?.Name;
                ApplicationInsights = environment.ApplicationInsightsAccount?.Name;
                KeyVault = environment.KeyVault?.Uri;
                VNet = environment.Subnet?.VNetName;

                if (environment.DeletionSettings != null)
                {
                    DeleteResourceGroup = environment.DeletionSettings.DeleteResourceGroup;
                    DeleteBatchAccount = environment.DeletionSettings.DeleteBatchAccount;
                    DeleteStorageAccount = environment.DeletionSettings.DeleteStorageAccount;
                    DeleteAppInsights = environment.DeletionSettings.DeleteAppInsights;
                    DeleteKeyVault = environment.DeletionSettings.DeleteKeyVault;
                    DeleteVNet = environment.DeletionSettings.DeleteVNet;
                }
            }
        }

        // Details

        public Guid? SubscriptionId { get; set; }

        public string LocationName { get; set; }

        public string ResourceGroup { get; set; }

        public string BatchAccount { get; set; }

        public string StorageAccount { get; set; }

        public string ApplicationInsights { get; set; }

        public string KeyVault { get; set; }

        public string VNet { get; set; }

        // Delete Options

        public bool DeleteResourceGroup { get; set; }

        public bool DeleteBatchAccount { get; set; }

        public bool DeleteStorageAccount { get; set; }

        public bool DeleteAppInsights { get; set; }

        public bool DeleteKeyVault { get; set; }

        public bool DeleteVNet { get; set; }
    }
}
