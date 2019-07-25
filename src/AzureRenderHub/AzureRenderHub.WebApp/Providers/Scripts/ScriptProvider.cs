using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebApp.Config;

namespace AzureRenderHub.WebApp.Providers.Scripts
{
    public class ScriptProvider : IScriptProvider
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly string _scriptsDirectoryName;

        public ScriptProvider(
            IHostingEnvironment hostingEnvironment,
            string scriptsDirectoryName = "Scripts")
        {
            _hostingEnvironment = hostingEnvironment;
            _scriptsDirectoryName = scriptsDirectoryName;
        }

        public IEnumerable<FileInfo> GetBundledScripts(RenderManagerType renderManager)
        {
            var generalScriptsPath = GeneralScriptsDirectory;
            var renderManagerScritpsPath = GetRenderManagerScriptsDirectory(renderManager);

            var files = new List<FileInfo>();

            if (Directory.Exists(generalScriptsPath))
            {
                files.AddRange(Directory.EnumerateFiles(generalScriptsPath).Select(f => new FileInfo(f)));
            }

            if (Directory.Exists(renderManagerScritpsPath))
            {
                files.AddRange(Directory.EnumerateFiles(renderManagerScritpsPath).Select(f => new FileInfo(f)));
            }

            return files;
        }

        private string ScriptRootDirectory => 
            Path.Combine(_hostingEnvironment.ContentRootPath, _scriptsDirectoryName);

        private string GeneralScriptsDirectory => Path.Combine(ScriptRootDirectory, "General");

        private string GetRenderManagerScriptsDirectory(RenderManagerType renderManager)
        {
            switch (renderManager)
            {
                case RenderManagerType.Deadline: return Path.Combine(ScriptRootDirectory, "Deadline");
                case RenderManagerType.Qube610:
                case RenderManagerType.Qube70: return Path.Combine(ScriptRootDirectory, "Qube");
                case RenderManagerType.Tractor: return Path.Combine(ScriptRootDirectory, "Tractor");
            }
            throw new ArgumentException($"Unsupported render manager type {renderManager}", "renderManager");
        }
    }
}
