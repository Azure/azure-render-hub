// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using WebApp.Code.Contract;

namespace WebApp.Config.Coordinators
{
    public class PackageCoordinator : IPackageCoordinator
    {
        private readonly IGenericConfigCoordinator _configCoordinator;
        private readonly CloudBlobContainer _container;

        public PackageCoordinator(IGenericConfigCoordinator configCoordinator, CloudBlobContainer container)
        {
            _configCoordinator = configCoordinator;
            _container = container;
        }

        public async Task<List<string>> ListPackages()
        {
            return await _configCoordinator.List(_container);
        }

        public async Task<InstallationPackage> GetPackage(string packageName)
        {
            return await _configCoordinator.Get<InstallationPackage>(_container, packageName);
        }

        public async Task UpdatePackage(InstallationPackage package)
        {
            await _configCoordinator.Update(_container, package, package.PackageName);
        }

        public async Task<bool> RemovePackage(InstallationPackage package)
        {
            return await _configCoordinator.Remove(_container, package.PackageName);
        }
    }
}
