// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Code.Contract;

namespace WebApp.Config.Coordinators
{
    public class PackageCoordinator : IPackageCoordinator
    {
        private readonly IGenericConfigCoordinator _configCoordinator;

        public PackageCoordinator(IGenericConfigCoordinator configCoordinator)
        {
            _configCoordinator = configCoordinator;
        }

        public async Task<List<string>> ListPackages()
        {
            return await _configCoordinator.List();
        }

        public async Task<InstallationPackage> GetPackage(string packageName)
        {
            return await _configCoordinator.Get<InstallationPackage>(packageName);
        }

        public async Task UpdatePackage(InstallationPackage package)
        {
            await _configCoordinator.Update(package, package.PackageName);
        }

        public async Task<bool> RemovePackage(InstallationPackage package)
        {
            return await _configCoordinator.Remove(package.PackageName);
        }
    }
}
