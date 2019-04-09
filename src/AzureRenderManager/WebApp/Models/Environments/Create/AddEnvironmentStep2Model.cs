// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using WebApp.Code;
using WebApp.Config;

namespace WebApp.Models.Environments.Create
{
    public class AddEnvironmentStep2Model : EnvironmentBaseModel
    {
        // needs this empty constructor for model bindings
        public AddEnvironmentStep2Model() { }

        public AddEnvironmentStep2Model(RenderingEnvironment environment)
        {
            EnvironmentName = environment.Name;
            SubscriptionId = environment.SubscriptionId;
            LocationName = environment.LocationName;
            RenderManager = environment.RenderManager;

            if (!string.IsNullOrEmpty(environment.ResourceGroupName))
            {
                NewResourceGroupName = null;
                ExistingResourceGroupNameAndLocation 
                    = $"{environment.ResourceGroupName};{environment.LocationName}";
            }
            else
            {
                NewResourceGroupName = EnvironmentName + "-rg";
            }
            
            if (environment.KeyVault != null)
            {
                NewKeyVaultName = null;
                ExistingKeyVaultIdLocationAndUri 
                    = $"{environment.KeyVault.ResourceId};{environment.KeyVault.Location};{environment.KeyVault.Uri}";
            }
            else
            {
                var kvName = EnvironmentName.Length > 21 ? EnvironmentName.Substring(0, 21) : EnvironmentName;
                NewKeyVaultName = $"{Regex.Replace(kvName, "/[&\\/\\\\_-]/g", "")}-kv";
            }

            if (environment.BatchAccount != null)
            {
                NewBatchAccountName = null;
                BatchAccountResourceIdLocationUrl
                    = $"{environment.BatchAccount.ResourceId};{environment.BatchAccount.Location};{environment.BatchAccount.Url}";
            }

            if (environment.StorageAccount != null)
            {
                NewStorageAccountName = null;
                StorageAccountResourceIdAndLocation
                    = $"{environment.StorageAccount.ResourceId};{environment.StorageAccount.Location}";
            }

            if (environment.Subnet?.ResourceId != null)
            {
                NewVnetName = null;
                SubnetResourceIdLocationAndAddressPrefix = environment.Subnet.ToString();
            }

            if (environment.ApplicationInsightsAccount?.ResourceId != null)
            {
                NewApplicationInsightsName = null;
                ApplicationInsightsIdAndLocation
                    = $"{environment.ApplicationInsightsAccount.ResourceId};{environment.ApplicationInsightsAccount.Location}";
            }
        }

        [Required]
        public Guid SubscriptionId { get; set; }

        [Required]
        public string LocationName { get; set; }

        [RegularExpression(Validation.RegularExpressions.ResourceGroup, ErrorMessage = Validation.Errors.Regex.ResourceGroup)]
        [StringLength(64)]
        public string NewResourceGroupName { get; set; }

        public string ExistingResourceGroupNameAndLocation { get; set; }

        [RegularExpression(Validation.RegularExpressions.KeyVault, ErrorMessage = "The Key Vault name must begin with a letter, end with a letter or digit, and not contain consecutive hyphens.")]
        [StringLength(24, MinimumLength = 3, ErrorMessage = "Key Vault name must be between 3 and 24 characters")]
        public string NewKeyVaultName { get; set; }

        public string ExistingKeyVaultIdLocationAndUri { get; set; }

        public string BatchAccountResourceIdLocationUrl { get; set; }

        [RegularExpression(Validation.RegularExpressions.BatchAccountName, ErrorMessage = "Batch account name can only contain lowercase letters and numbers.")]
        [StringLength(24, MinimumLength = 3, ErrorMessage = "Batch account name must be between 3 and 24 characters")]
        public string NewBatchAccountName { get; set; }

        public string StorageAccountResourceIdAndLocation { get; set; }

        [RegularExpression(Validation.RegularExpressions.StorageAccountName, ErrorMessage = "Storage account name can only contain lowercase letters and numbers.")]
        [StringLength(24, MinimumLength = 3, ErrorMessage = "Storage account name must be between 3 and 24 characters")]
        public string NewStorageAccountName { get; set; }

        [RegularExpression(Validation.RegularExpressions.FileShareName, ErrorMessage = "File share name must start with a letter or number, and can contain only letters, numbers, and the dash (-) character.")]
        [StringLength(63, MinimumLength = 3, ErrorMessage = "File share name must be between 3 and 63 characters")]
        public string NewFileShareName { get; set; }

        /// <summary>
        /// Semi-colon delimited resource id, location and address prefix
        /// </summary>
        public string SubnetResourceIdLocationAndAddressPrefix { get; set; }

        [RegularExpression(Validation.RegularExpressions.VNetName, ErrorMessage = "VNet name can only contain letters, numbers, underscores, periods, or hyphens.")]
        [StringLength(64, MinimumLength = 0, ErrorMessage = "VNet name must be between 2 and 64 characters")]
        public string NewVnetName { get; set; }

        /// <summary>
        /// Semi-colon delimited resource id and location
        /// </summary>
        public string ApplicationInsightsIdAndLocation { get; set; }

        [RegularExpression(Validation.RegularExpressions.AppInsightsName, ErrorMessage = "Application Insights name can only contain letters, numbers, underscores, periods, or hyphens.")]
        [StringLength(64, MinimumLength = 0, ErrorMessage = "Application Insights name must be between 2 and 64 characters")]
        public string NewApplicationInsightsName { get; set; }

        public string NewApplicationInsightsLocation { get; set; }

        public string Error { get; set; }

        public string ErrorMessage { get; set; }
    }
}
