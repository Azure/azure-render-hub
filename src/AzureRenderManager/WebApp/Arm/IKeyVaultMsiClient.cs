// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using WebApp.Config;

namespace WebApp.Arm
{
    public interface IKeyVaultMsiClient
    {
        Task<X509Certificate2> GetKeyVaultCertificateAsync(
            Guid subscriptionId,
            KeyVault keyVault,
            string certificateName,
            string password);

        Task<string> GetKeyVaultSecretAsync(
            Guid subscriptionId,
            KeyVault keyVault,
            string secretName);

        Task SetKeyVaultSecretAsync(
            Guid subscriptionId,
            KeyVault keyVault,
            string secretName,
            string value);

        Task DeleteSecretAsync(
            Guid subscriptionId,
            KeyVault keyVault,
            string secretName);

        Task ImportKeyVaultCertificateAsync(
            Guid subscriptionId,
            KeyVault keyVault,
            string certificateName,
            byte[] value,
            string certificatePassword);
    }
}
