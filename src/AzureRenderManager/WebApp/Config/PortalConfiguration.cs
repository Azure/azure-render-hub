// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using WebApp.Config.Storage;

namespace WebApp.Config
{
    public class PortalConfiguration
    {
        public List<InstallationPackage> InstallationPackages { get; set; } = new List<InstallationPackage>();

        public List<AssetRepository> Repositories { get; set; } = new List<AssetRepository>();
    }
}
