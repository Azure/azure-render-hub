using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace WebApp.Config.Coordinators
{
    public interface IGenericConfigCoordinator
    {
        Task<T> Get<T>(CloudBlobContainer container, string configName);

        Task<bool> Remove(CloudBlobContainer container, string configName);

        Task Update<T>(CloudBlobContainer container, T config, string configName, string originalName = null);

        Task<List<string>> List(CloudBlobContainer container);
    }
}
