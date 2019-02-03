// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Config
{
    public interface IConfigurationStore
    {
        Task Set(PortalConfiguration configuration);

        Task<PortalConfiguration> Get();

        Task Delete();
    }
}
