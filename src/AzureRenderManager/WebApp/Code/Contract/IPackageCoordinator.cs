// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApp.Config;

namespace WebApp.Code.Contract
{
    public interface IPackageCoordinator
    {
        Task<List<string>> ListPackages();

        Task<InstallationPackage> GetPackage(string packageName);

        Task UpdatePackage(InstallationPackage package);

        Task<bool> RemovePackage(InstallationPackage package);
    }
}