﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.AppInsights
{
    public interface IAppInsightsQueryProvider
    {
        Task<AppInsightsQueryResult> ExecuteQuery(string applicationId, string apiKey, string query);
    }
}
