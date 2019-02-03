// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Threading.Tasks;

namespace WebApp.Config
{
    public interface IPortalConfigurationProvider
    {
        Task<PortalConfiguration> GetConfig();

        Task SetConfig(PortalConfiguration config);

        Task DeleteConfig();

        Task<bool> IsConfigured();
    }
}
