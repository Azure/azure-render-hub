// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WebApp.Code.JsonConverters;

namespace WebApp.Config
{
    public class BlobConfigurationStore : IConfigurationStore
    {
        private const string ConfigContainer = "config";
        private const string ConfigBlob = "config.json";

        private readonly CloudBlobClient _blobClient;

        public BlobConfigurationStore(CloudBlobClient blobClient)
        {
            _blobClient = blobClient;
        }

        public async Task Set(PortalConfiguration configuration)
        {
            var configContainer = _blobClient.GetContainerReference(ConfigContainer);
            await configContainer.CreateIfNotExistsAsync();
            var configBlob = configContainer.GetBlockBlobReference(ConfigBlob);
            await configBlob.UploadTextAsync(JsonConvert.SerializeObject(configuration, Formatting.Indented));
        }

        public async Task<PortalConfiguration> Get()
        {
            try
            {
                var configContainer = _blobClient.GetContainerReference(ConfigContainer);
                var configBlob = configContainer.GetBlockBlobReference(ConfigBlob);
                var serializerSettings = new JsonSerializerSettings();
                serializerSettings.Converters.Add(new StringEnumConverter());
                serializerSettings.Converters.Add(new AssetRepositoryConverter());

                return JsonConvert.DeserializeObject<PortalConfiguration>(await configBlob.DownloadTextAsync(), serializerSettings);
            }
            catch (StorageException se) when (se.RequestInformation.HttpStatusCode == 404)
            {
                // handle both container not found and blob not found
                return null;
            }
        }

        public async Task Delete()
        {
            var config = await Get();
            if (config != null)
            {
                if (config.InstallationPackages != null)
                {
                    foreach (var configInstallationPackage in config.InstallationPackages)
                    {
                        if (!string.IsNullOrEmpty(configInstallationPackage.Container))
                        {
                            var container = _blobClient.GetContainerReference(configInstallationPackage.Container);
                            await container.DeleteIfExistsAsync();
                        }
                    }
                }

                var configContainer = _blobClient.GetContainerReference(ConfigContainer);
                if (await configContainer.ExistsAsync())
                {
                    var configBlob = configContainer.GetBlockBlobReference(ConfigBlob);
                    await configBlob.DeleteIfExistsAsync();
                }
            }
        }
    }
}
