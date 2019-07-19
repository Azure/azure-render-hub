// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WebApp.Providers.Templates
{
    public interface ITemplateProvider
    {
        Task<JObject> GetTemplate(string templateName);

        Dictionary<string, Dictionary<string, object>> GetParameters(Dictionary<string, object> parameters);
    }
}
