// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.Identity.Web.Client;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using WebApp.Code.Contract;
using WebApp.Config;
using WebApp.Config.Storage;

namespace WebApp.Controllers
{
    public class MenuBaseController : ViewBaseController
    {
        protected readonly IAssetRepoCoordinator _assetRepoCoordinator;
        protected readonly IEnvironmentCoordinator _environmentCoordinator;
        private readonly IPackageCoordinator _packageCoordinator;

        public MenuBaseController(
            IEnvironmentCoordinator environmentCoordinator,
            IPackageCoordinator packageCoordinator,
            IAssetRepoCoordinator assetRepoCoordinator,
            ITokenAcquisition tokenAcquisition) : base(tokenAcquisition)
        {
            _packageCoordinator = packageCoordinator;
            _environmentCoordinator = environmentCoordinator;
            _assetRepoCoordinator = assetRepoCoordinator;
        }

        public Task<RenderingEnvironment> Environment(string envId)
            => _environmentCoordinator.GetEnvironment(envId);

        public async Task<IReadOnlyList<RenderingEnvironment>> Environments()
        {
            var envs = await Task.WhenAll((await _environmentCoordinator.ListEnvironments())
                .Select(env => _environmentCoordinator.GetEnvironment(env)));

            return envs.Where(re => re != null).OrderBy(re => re.Name).ToList();
        }

        public async Task<IReadOnlyList<InstallationPackage>> Packages()
        {
            var packages = await Task.WhenAll((await _packageCoordinator.ListPackages())
                .Select(packageName => _packageCoordinator.GetPackage(packageName)));

            return packages.Where(re => re != null).OrderBy(re => re.PackageName).ToList();
        }

        public async Task<IReadOnlyList<AssetRepository>> Repositories()
        {
            var repos = await Task.WhenAll((await _assetRepoCoordinator.ListRepositories())
                .Select(repoName => _assetRepoCoordinator.GetRepository(repoName)));

            return repos.Where(re => re != null).OrderBy(re => re.Name).ToList();
        }
    }
}
