// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.ComponentModel.DataAnnotations;

using WebApp.Config;

namespace WebApp.Models.Environments.Create
{
    public class AddEnvironmentStep4Model : EnvironmentBaseModel
    {
        private const string DefaultKeyVaultCertName = "RenderFarmManagerKVSPCert";

        // needs this empty constructor for model bindings
        public AddEnvironmentStep4Model() { }

        public AddEnvironmentStep4Model(RenderingEnvironment environment)
        {
            EnvironmentName = environment.Name;
            KeyVaultServicePrincipalCertificateName = DefaultKeyVaultCertName;
            RenderManager = environment.RenderManager;

            if (environment.KeyVaultServicePrincipal != null)
            {
                KeyVaultServicePrincipalAppId = environment.KeyVaultServicePrincipal.ApplicationId;
                KeyVaultServicePrincipalObjectId = environment.KeyVaultServicePrincipal.ObjectId;

                if (!string.IsNullOrEmpty(environment.KeyVaultServicePrincipal.CertificateKeyVaultName))
                {
                    KeyVaultServicePrincipalCertificateName = environment.KeyVaultServicePrincipal.CertificateKeyVaultName;
                }
            }

            SubscriptionId = environment.SubscriptionId;
            KeyVaultName = environment.KeyVault?.Name;
        }

        [Required]
        public Guid? KeyVaultServicePrincipalAppId { get; set; }

        [Required]
        public Guid? KeyVaultServicePrincipalObjectId { get; set; }

        [Required]
        public string KeyVaultServicePrincipalCertificateName { get; set; }

        public Guid SubscriptionId { get; }

        public string KeyVaultName { get; }

        public string Error { get; set; }

        public string ErrorMessage { get; set; }
    }
}
