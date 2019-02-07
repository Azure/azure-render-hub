using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WebApp.Code.JsonConverters;

namespace WebApp.Config.Coordinators
{
    public class GenericConfigCoordinator : IGenericConfigCoordinator
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public GenericConfigCoordinator()
        {
            _serializerSettings = new JsonSerializerSettings();
            _serializerSettings.Converters.Add(new StringEnumConverter());
            _serializerSettings.Converters.Add(new AssetRepositoryConverter());
        }

        public async Task<T> Get<T>(CloudBlobContainer container, string configName)
        {
            try
            {
                var content = await BlobFor(container, configName).DownloadTextAsync();
                return JsonConvert.DeserializeObject<T>(content, _serializerSettings);
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                // handle both blob does not exist and container does not exist
                return default(T);
            }
        }

        public async Task<bool> Remove(CloudBlobContainer container, string configName)
        {
            return await BlobFor(container, configName).DeleteIfExistsAsync();
        }

        public async Task Update<T>(CloudBlobContainer container, T config, string configName, string originalName = null)
        {
            var content = JsonConvert.SerializeObject(config, Formatting.Indented);
            await HandleContainerDoesNotExist(container, () => BlobFor(container, configName).UploadTextAsync(content));
            if (originalName != null && originalName != configName)
            {
                await BlobFor(container, originalName).DeleteIfExistsAsync();
            }
        }

        public async Task<List<string>> List(CloudBlobContainer container)
        {
            var result = new List<string>();

            BlobContinuationToken token = null;
            try
            {
                do
                {
                    var page = await container.ListBlobsSegmentedAsync(token);
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

        private CloudBlockBlob BlobFor(CloudBlobContainer container, string configName)
            => container.GetBlockBlobReference(configName);

        private async Task HandleContainerDoesNotExist(CloudBlobContainer container, Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (StorageException ex) when (ex.RequestInformation.ErrorCode == BlobErrorCodeStrings.ContainerNotFound)
            {
                await container.CreateIfNotExistsAsync();
                await action();
            }
        }
    }
}
