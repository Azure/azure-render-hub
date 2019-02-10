// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using WebApp.Arm;
using WebApp.Code.Attributes;
using WebApp.Config.RenderManager;

namespace WebApp.Config.Coordinators
{
    public class EnvironmentSecretsRepository : IConfigRepository<RenderingEnvironment>
    {
        private readonly IKeyVaultMsiClient _keyVaultClient;
        private readonly ILogger<EnvironmentSecretsRepository> _logger;
        private readonly IConfigRepository<RenderingEnvironment> _configCoordinator;

        public EnvironmentSecretsRepository(
            IConfigRepository<RenderingEnvironment> configCoordinator,
            IKeyVaultMsiClient keyVaultClient,
            ILogger<EnvironmentSecretsRepository> logger)
        {
            _configCoordinator = configCoordinator;
            _keyVaultClient = keyVaultClient;
            _logger = logger;
        }

        public async Task<RenderingEnvironment> Get(string environmentName)
        {
            try
            {
                var result = await _configCoordinator.Get(environmentName);

                if (result != null && result.KeyVault != null)
                {
                    try
                    {
                        await FindAndGetCredentials(result, result);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Unable to load credentials");
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

        public async Task<bool> Remove(string environmentName)
        {
            return await _configCoordinator.Remove(environmentName);
        }

        public async Task Update(RenderingEnvironment environment, string newName, string originalName = null)
        {
            await _configCoordinator.Update(environment, newName, originalName);

            if (environment.KeyVault != null)
            {
                try
                {
                    await FindAndSaveCredentials(environment, environment);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unable to save credentials");
                }
            }
        }

        public async Task<List<string>> List()
        {
            return await _configCoordinator.List();
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
