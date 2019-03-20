// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;
using WebApp.Config;

namespace WebApp.Arm
{
    public class KeyVaultMsiClient : IKeyVaultMsiClient
    {
        public async Task<X509Certificate2> GetKeyVaultCertificateAsync(
            Guid subscriptionId,
            KeyVault keyVault,
            string certificateName,
            string password)
        {
            // Authenticate using MSI
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net");
            var token = new TokenCredentials(accessToken);
            var keyVaultClient = new KeyVaultClient(token);
            try
            {
                var certSecret = await keyVaultClient.GetSecretAsync(keyVault.Uri, certificateName);
                if (certSecret == null)
                {
                    return null;
                }

                return new X509Certificate2(
                    Convert.FromBase64String(certSecret.Value),
                    string.Empty,
                    X509KeyStorageFlags.MachineKeySet |
                    X509KeyStorageFlags.PersistKeySet |
                    X509KeyStorageFlags.Exportable);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to get certificate with principal: {azureServiceTokenProvider.PrincipalUsed}", e);
            }
        }

        public async Task<string> GetKeyVaultSecretAsync(
            Guid subscriptionId,
            KeyVault keyVault,
            string secretName)
        {
            // Authenticate using MSI
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net");
            var token = new TokenCredentials(accessToken);
            var keyVaultClient = new KeyVaultClient(token);
            try
            {
                var secret = await keyVaultClient.GetSecretAsync(keyVault.Uri, secretName);
                return secret?.Value;
            }
            catch (KeyVaultErrorException e)
            {
                if (e.Body?.Error?.Code != "SecretNotFound")
                {
                    throw new Exception($"Failed to get secret with principal: {azureServiceTokenProvider.PrincipalUsed}", e);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to get secret with principal: {azureServiceTokenProvider.PrincipalUsed}", e);
            }

            return null;
        }


        public async Task SetKeyVaultSecretAsync(Guid subscriptionId, KeyVault keyVault, string secretName, string value)
        {
            // Authenticate using MSI
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net");
            var token = new TokenCredentials(accessToken);
            var keyVaultClient = new KeyVaultClient(token);
            try
            {
                await keyVaultClient.SetSecretAsync(keyVault.Uri, secretName, value);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to set secret with principal: {azureServiceTokenProvider.PrincipalUsed}", e);
            }
        }

        public async Task DeleteSecretAsync(Guid subscriptionId, KeyVault keyVault, string secretName)
        {
            // Authenticate using MSI
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net");
            var token = new TokenCredentials(accessToken);
            var keyVaultClient = new KeyVaultClient(token);
            try
            {
                await keyVaultClient.DeleteSecretAsync(keyVault.Uri, secretName);
            }
            catch (KeyVaultErrorException ex)
            {
                if (ex.Body?.Error?.Code != "SecretNotFound")
                {
                    throw new Exception($"Failed to set secret with principal: {azureServiceTokenProvider.PrincipalUsed}", ex);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to set secret with principal: {azureServiceTokenProvider.PrincipalUsed}", e);
            }
        }

        public async Task ImportKeyVaultCertificateAsync(Guid subscriptionId, KeyVault keyVault, string certificateName, byte[] value, string certificatePassword)
        {
            // Authenticate using MSI
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net");
            var token = new TokenCredentials(accessToken);
            var keyVaultClient = new KeyVaultClient(token);
            try
            {
                if (certificatePassword == null)
                {
                    certificatePassword = string.Empty;
                }

                var cert = new X509Certificate2(
                    value,
                    certificatePassword,
                    X509KeyStorageFlags.MachineKeySet |
                    X509KeyStorageFlags.PersistKeySet |
                    X509KeyStorageFlags.Exportable);

                var importBase64 = Convert.ToBase64String(value);
                Console.WriteLine(importBase64);

                var exportedBytes = cert.Export(X509ContentType.Pkcs12, certificatePassword);
                var exportedBase64 = Convert.ToBase64String(exportedBytes);

                await keyVaultClient.ImportCertificateAsync(
                    keyVault.Uri,
                    certificateName,
                    Convert.ToBase64String(value),
                    certificatePassword);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to set secret with principal: {azureServiceTokenProvider.PrincipalUsed}", e);
            }
        }
    }
}
