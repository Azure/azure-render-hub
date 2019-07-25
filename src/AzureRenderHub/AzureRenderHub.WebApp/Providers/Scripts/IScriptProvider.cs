using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Config;

namespace AzureRenderHub.WebApp.Providers.Scripts
{
    public interface IScriptProvider
    {
        IEnumerable<FileInfo> GetBundledScripts(RenderManagerType renderManager);
    }
}
