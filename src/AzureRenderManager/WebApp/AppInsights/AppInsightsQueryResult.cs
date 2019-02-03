// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WebApp.AppInsights
{
    public class AppInsightsQueryResult
    {
        public bool Success { get; set; }

        public string Error { get; set; }

        public JObject Results { get; set; }
    }
}
