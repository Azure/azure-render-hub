// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApp.Config;

namespace WebApp.Code.Contract
{
    public interface IPackageCoordinator
    {
        Task<List<InstallationPackage>> GetPackages();

        Task<InstallationPackage> GetPackage(string packageName);

        Task UpdatePackage(InstallationPackage package);

        Task UpdatePackages(IEnumerable<InstallationPackage> packages);

        Task<bool> RemovePackage(InstallationPackage package);

        void ClearCache();
    }
}