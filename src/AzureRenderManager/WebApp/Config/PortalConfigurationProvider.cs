// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Threading.Tasks;
using WebApp.Arm;

namespace WebApp.Config
{
    public class PortalConfigurationProvider : IPortalConfigurationProvider
    {
        private readonly BlobConfigurationStore _blobConfigurationStore;

        public PortalConfigurationProvider(BlobConfigurationStore blobConfigStore)
        {
            _blobConfigurationStore = blobConfigStore;
        }

        public Task<PortalConfiguration> GetConfig()
        {
            return _blobConfigurationStore.Get();
        }

        public async Task SetConfig(PortalConfiguration config)
        {
            await _blobConfigurationStore.Set(config);
        }

        public async Task DeleteConfig()
        {
            await _blobConfigurationStore.Delete();
        }

        public async Task<bool> IsConfigured()
        {
            return await GetConfig() != null;
        }
    }
}