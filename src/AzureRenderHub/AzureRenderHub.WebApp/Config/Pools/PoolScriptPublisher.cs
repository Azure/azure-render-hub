using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureRenderHub.WebApp.Config.Pools
{
    public class PoolScriptPublisher : IPoolScriptPublisher
    {
        private readonly CloudBlobClient _blobClient;

        public PoolScriptPublisher(CloudBlobClient blobClient)
        {
            _blobClient = blobClient;
        }

        public async Task UploadScripts(string containerName, IEnumerable<FileInfo> files)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException("containerName");
            }

            if (files == null)
            {
                throw new ArgumentNullException("files");
            }

            var container = _blobClient.GetContainerReference(containerName);

            await container.CreateIfNotExistsAsync();

            var tasks = new List<Task>();

            foreach (var file in files)
            {
                var blob = container.GetBlockBlobReference(file.Name);
                tasks.Add(blob.UploadFromFileAsync(file.FullName));
            }

            await Task.WhenAll(tasks);
        }

        public async Task DeleteContainer(string containerName)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException("containerName");
            }

            var container = _blobClient.GetContainerReference(containerName);

            await container.DeleteIfExistsAsync();
        }
    }
}
