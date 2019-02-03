// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Newtonsoft.Json;
using WebApp.Arm;
using WebApp.Code.Attributes;
using WebApp.Code.Contract;
using WebApp.Config.RenderManager;

namespace WebApp.Config.Coordinators
{
    public class EnvironmentCoordinator : IEnvironmentCoordinator
    {
        private readonly IKeyVaultMsiClient _keyVaultClient;
        private readonly CloudBlobContainer _environmentContainer;

        public EnvironmentCoordinator(CloudBlobContainer environmentContainer, IKeyVaultMsiClient keyVaultClient)
        {
            _environmentContainer = environmentContainer;
            _keyVaultClient = keyVaultClient;
        }

        private CloudBlockBlob BlobFor(string environmentName)
            => _environmentContainer.GetBlockBlobReference(environmentName);

        private async Task HandleContainerDoesNotExist(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (StorageException ex) when (ex.RequestInformation.ErrorCode == BlobErrorCodeStrings.ContainerNotFound)
            {
                await _environmentContainer.CreateIfNotExistsAsync();
                await action();
            }
        }

        public async Task<RenderingEnvironment> GetEnvironment(string environmentName)
        {
            try
            {
                var content = await BlobFor(environmentName).DownloadTextAsync();
                var result = JsonConvert.DeserializeObject<RenderingEnvironment>(content);

                if (result.KeyVault != null)
                {
                    try
                    {
                        await FindAndGetCredentials(result, result);
                    }
                    catch (Exception e)
                    {
                        // TODO: ???
                        Console.WriteLine(e);
                    }
                }

                return result;
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                // handle both blob does not exist and container does not exist
                return null;
            }
        }

        public Task<bool> RemoveEnvironment(RenderingEnvironment environment)
        {
            return BlobFor(environment.Name).DeleteIfExistsAsync();
        }

        public async Task UpdateEnvironment(RenderingEnvironment environment, string originalName = null)
        {
            var content = JsonConvert.SerializeObject(environment, Formatting.Indented);
            await HandleContainerDoesNotExist(() => BlobFor(environment.Name).UploadTextAsync(content));

            if (environment.KeyVault != null)
            {
                try
                {
                    await FindAndSaveCredentials(environment, environment);
                }
                catch (Exception e)
                {
                    // TODO: ???
                    Console.WriteLine(e);
                }
            }

            if (originalName != null && originalName != environment.Name)
            {
                await BlobFor(originalName).DeleteIfExistsAsync();
            }
        }

        public async Task<List<string>> ListEnvironments()
        {
            var result = new List<string>();

            BlobContinuationToken token = null;
            try
            {
                do
                {
                    var page = await _environmentContainer.ListBlobsSegmentedAsync(token);

                    result.AddRange(page.Results.OfType<CloudBlockBlob>().Select(b => b.Name));
                    token = page.ContinuationToken;
                } while (token != null);

                return result;
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                // nothing found
                return result;
            }
        }

        private async Task FindAndSaveCredentials(RenderingEnvironment environment, object obj)
        {
            if (obj == null) return;

            Type objType = obj.GetType();
            PropertyInfo[] properties = objType.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                object propValue = property.GetValue(obj, null);

                if (property.PropertyType == typeof(string))
                {
                    if (TryGetAttribute<CredentialAttribute>(property, out var attribute))
                    {
                        var value = (string)propValue;

                        if (string.IsNullOrEmpty(value))
                        {
                            await _keyVaultClient.DeleteSecretAsync(
                                environment.KeyVault.SubscriptionId,
                                environment.KeyVault,
                                attribute.Name);
                        }
                        else
                        {
                            await _keyVaultClient.SetKeyVaultSecretAsync(
                                environment.KeyVault.SubscriptionId,
                                environment.KeyVault,
                                attribute.Name,
                                value);
                        }
                    }
                }
                else if (property.PropertyType == typeof(Certificate))
                {
                    var cert = (Certificate)propValue;
                    if (TryGetAttribute<CredentialAttribute>(property, out var attribute))
                    {
                        if (cert.CertificateData != null && cert.CertificateData.Length > 0)
                        {
                            await _keyVaultClient.ImportKeyVaultCertificateAsync(
                                environment.KeyVault.SubscriptionId,
                                environment.KeyVault,
                                attribute.Name,
                                cert.CertificateData,
                                cert.Password);
                        }

                        // Continue to persist the password
                        await FindAndSaveCredentials(environment, propValue);
                    }
                }
                else if (property.PropertyType.Assembly == objType.Assembly)
                {
                    await FindAndSaveCredentials(environment, propValue);
                }
                else
                {
                    var elems = propValue as IList;
                    if (elems != null)
                    {
                        foreach (var item in elems)
                        {
                            await FindAndSaveCredentials(environment, item);
                        }
                    }
                }
            }
        }

        private async Task FindAndGetCredentials(RenderingEnvironment environment, object obj)
        {
            if (obj == null) return;

            Type objType = obj.GetType();
            if (objType == typeof(string)) return;

            PropertyInfo[] properties = objType.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                object propValue = property.GetValue(obj, null);

                if (property.PropertyType == typeof(string))
                {
                    if (TryGetAttribute<CredentialAttribute>(property, out var attribute))
                    {
                        var cred = await _keyVaultClient.GetKeyVaultSecretAsync(
                            environment.KeyVault.SubscriptionId,
                            environment.KeyVault,
                            attribute.Name);

                        property.SetValue(obj, cred);
                    }
                }
                else if (property.PropertyType.Assembly == objType.Assembly)
                {
                    await FindAndGetCredentials(environment, propValue);
                }
                else
                {
                    var elems = propValue as IList;
                    if (elems != null)
                    {
                        foreach (var item in elems)
                        {
                            await FindAndGetCredentials(environment, item);
                        }
                    }
                }
            }
        }

        private static bool TryGetAttribute<T>(PropertyInfo propertyInfo, out T customAttribute) where T : Attribute
        {
            customAttribute = propertyInfo.GetCustomAttribute<T>(false);
            return customAttribute != null;
        }
    }
}
