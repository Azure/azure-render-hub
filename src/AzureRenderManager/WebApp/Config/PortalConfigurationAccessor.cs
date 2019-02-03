// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApp.Config
{
    public sealed class PortalConfigurationAccessor
    {
        private readonly Lazy<Task<PortalConfiguration>> _config;

        public PortalConfigurationAccessor(IPortalConfigurationProvider configMgr)
        {
            _config = new Lazy<Task<PortalConfiguration>>(async () => await configMgr.GetConfig());
        }

        public Task<PortalConfiguration> Value => _config.Value;
    }
}
