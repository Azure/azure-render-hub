// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using WebApp.Code;
using WebApp.Code.Contract;
using WebApp.Code.Extensions;

namespace WebApp.Config.Coordinators
{
    public class PackageCoordinator : IPackageCoordinator
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPortalConfigurationProvider _portalConfigurationProvider;

        public PackageCoordinator(IPortalConfigurationProvider portalConfigurationProvider, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _portalConfigurationProvider = portalConfigurationProvider;
        }

        public async Task<List<InstallationPackage>> GetPackages()
        {
            var cached = _httpContextAccessor.HttpContext.Session.Get<List<InstallationPackage>>(CacheKeys.PackageList);
            if (cached != null)
            {
                return cached;
            }

            var config = await _portalConfigurationProvider.GetConfig();
            var packages = config != null ? config.InstallationPackages : new List<InstallationPackage>();
            _httpContextAccessor.HttpContext.Session.Set(CacheKeys.PackageList, packages);

            return packages;
        }

        public async Task<InstallationPackage> GetPackage(string packageName)
        {
            var packages = await GetPackages();
            var found = packages?.FirstOrDefault(pkg => pkg.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            return found;
        }

        public async Task UpdatePackage(InstallationPackage package)
        {
            var packages = await GetPackages() ?? new List<InstallationPackage>();
            var index = packages.FindIndex(pkg => pkg.PackageName.Equals(package.PackageName, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                packages.Add(package);
            }
            else
            {
                packages[index] = package;
            }

            await UpdatePackages(packages);
        }

        public async Task UpdatePackages(IEnumerable<InstallationPackage> packages)
        {
            var config = await _portalConfigurationProvider.GetConfig() ?? new PortalConfiguration();
            config.InstallationPackages = packages.ToList();
            await _portalConfigurationProvider.SetConfig(config);
            _httpContextAccessor.HttpContext.Session.Set(CacheKeys.PackageList, config.InstallationPackages);
        }

        public async Task<bool> RemovePackage(InstallationPackage package)
        {
            var packages = await GetPackages();
            var index = packages?.FindIndex(pkg => pkg.PackageName.Equals(package.PackageName, StringComparison.OrdinalIgnoreCase));
            if (index.GetValueOrDefault(-1) > -1)
            {
                packages?.RemoveAt(index.Value);
                await UpdatePackages(packages);

                return true;
            }

            return false;
        }

        public void ClearCache()
        {
            _httpContextAccessor.HttpContext.Session.Remove(CacheKeys.PackageList);
        }
    }
}
