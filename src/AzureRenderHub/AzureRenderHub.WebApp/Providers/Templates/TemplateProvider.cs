// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;

namespace WebApp.Providers.Templates
{
    public class TemplateProvider: ITemplateProvider
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public TemplateProvider(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public async Task<JObject> GetTemplate(string templateName)
        {
            var file = new FileInfo(
                Path.Combine(
                    _hostingEnvironment.ContentRootPath,
                    "Templates",
                    templateName));
            return JObject.Parse(await File.ReadAllTextAsync(file.FullName));
        }

        public Dictionary<string, Dictionary<string, object>> GetParameters(Dictionary<string, object> parameters)
        {
            var outputParameters = new Dictionary<string, Dictionary<string, object>>();
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    outputParameters[parameter.Key] = new Dictionary<string, object> {{"value", parameter.Value}};
                }
            }
            return outputParameters;
        }
    }
}
