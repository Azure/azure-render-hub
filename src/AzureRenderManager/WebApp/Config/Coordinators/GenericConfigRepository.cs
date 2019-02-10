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
    // This stores the configs as JSON-serialized blobs in the specified container.
    public class GenericConfigRepository<T> : IConfigRepository<T>
    {
        private static readonly JsonSerializerSettings _serializerSettings
            = new JsonSerializerSettings
            {
                Converters =
                {
                    new StringEnumConverter(),
                    new AssetRepositoryConverter(),
                }
            };

        private readonly CloudBlobContainer _container;

        public GenericConfigRepository(CloudBlobContainer container)
        {
            _container = container;
        }

        public async Task<T> Get(string configName)
        {
            try
            {
                var content = await BlobFor(_container, configName).DownloadTextAsync();
                return JsonConvert.DeserializeObject<T>(content, _serializerSettings);
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                // handle both blob does not exist and container does not exist
                return default;
            }
        }

        public async Task<bool> Remove(string configName)
        {
            return await BlobFor(_container, configName).DeleteIfExistsAsync();
        }

        public async Task Update(T config, string configName, string originalName = null)
        {
            var content = JsonConvert.SerializeObject(config, Formatting.Indented);
            await HandleContainerDoesNotExist(() => BlobFor(_container, configName).UploadTextAsync(content));
            if (originalName != null && originalName != configName)
            {
                await BlobFor(_container, originalName).DeleteIfExistsAsync();
            }
        }

        public async Task<List<string>> List()
        {
            var result = new List<string>();

            BlobContinuationToken token = null;
            try
            {
                do
                {
                    var page = await _container.ListBlobsSegmentedAsync(token);
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

        private async Task HandleContainerDoesNotExist(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (StorageException ex) when (ex.RequestInformation.ErrorCode == BlobErrorCodeStrings.ContainerNotFound)
            {
                await _container.CreateIfNotExistsAsync();
                await action();
            }
        }
    }
}
