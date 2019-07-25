using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureRenderHub.WebApp.Config.Pools
{
    public interface IPoolScriptPublisher
    {
        Task UploadScripts(string containerName, IEnumerable<FileInfo> files);

        Task DeleteContainer(string containerName);
    }
}
