// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Batch.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using WebApp.Config.RenderManager;

namespace WebApp.Config.Pools
{
    public class StartTaskProvider
    {
        private const string JoinDomainScript = "https://raw.githubusercontent.com/Azure/azure-qube/master/Scripts/join-domain.ps1";

        private readonly IConfiguration _configuration;
        private readonly CloudBlobClient _blobClient;

        public StartTaskProvider(IConfiguration configuration, CloudBlobClient blobClient)
        {
            _configuration = configuration;
            _blobClient = blobClient;
        }

        public string GetWindowsStartTaskUrl(RenderingEnvironment environment)
        {
            var type = GetGenericRenderManagerType(environment.RenderManager);
            return string.IsNullOrEmpty(environment.WindowsBootstrapScript)
                ? _configuration[$"{type}:StartTaskScriptWindows"]
                : environment.WindowsBootstrapScript;
        }

        public string GetLinuxStartTaskUrl(RenderingEnvironment environment)
        {
            var type = GetGenericRenderManagerType(environment.RenderManager);
            return string.IsNullOrEmpty(environment.LinuxBootstrapScript)
                ? _configuration[$"{type}:StartTaskScriptLinux"]
                : environment.LinuxBootstrapScript;
        }

        private string GetGenericRenderManagerType(RenderManagerType type)
        {
            switch (type)
            {
                case RenderManagerType.Qube610:
                case RenderManagerType.Qube70: return "Qube";
            }
            return type.ToString();
        }

        public async Task<StartTask> GetQubeStartTask(
            string poolName,
            RenderingEnvironment environment,
            InstallationPackage qubePackage,
            InstallationPackage gpuPackage,
            IEnumerable<InstallationPackage> generalPackages,
            bool isWindows)
        {
            var resourceFiles = new List<ResourceFile>();

            var startTask = new StartTask(
                "",
                resourceFiles,
                GetEnvironmentSettings(environment),
                new UserIdentity(
                    autoUser: new AutoUserSpecification(AutoUserScope.Pool, ElevationLevel.Admin)),
                3, // retries
                true); // waitForSuccess

            await AppendGpu(startTask, gpuPackage);

            AppendDomainSetup(startTask, environment);

            await AppendQubeSetupToStartTask(
                environment,
                poolName,
                startTask,
                environment.RenderManagerConfig.Qube,
                qubePackage,
                isWindows);

            await AppendGeneralPackages(startTask, generalPackages);

            // Wrap all the start task command
            startTask.CommandLine = $"powershell.exe -ExecutionPolicy RemoteSigned -NoProfile \"$ErrorActionPreference='Stop'; {startTask.CommandLine}\"";

            return startTask;
        }

        public async Task<StartTask> GetDeadlineStartTask(
            string poolName,
            RenderingEnvironment environment,
            InstallationPackage deadlinePackage,
            InstallationPackage gpuPackage,
            IEnumerable<InstallationPackage> generalPackages,
            bool isWindows,
            bool useGroups)
        {
            if (environment == null ||
                environment.RenderManagerConfig == null ||
                environment.RenderManager != RenderManagerType.Deadline)
            {
                throw new Exception("Wrong environment for Deadline.");
            }

            var resourceFiles = new List<ResourceFile>();

            var startTask = new StartTask(
                "",
                resourceFiles,
                GetEnvironmentSettings(environment),
                new UserIdentity(
                    autoUser: new AutoUserSpecification(AutoUserScope.Pool, ElevationLevel.Admin)),
                3, // retries
                true); // waitForSuccess

            await AppendGpu(startTask, gpuPackage);

            await AppendDeadlineSetupToStartTask(
                environment,
                poolName,
                startTask,
                environment.RenderManagerConfig.Deadline,
                deadlinePackage,
                isWindows,
                useGroups);

            await AppendGeneralPackages(startTask, generalPackages);

            // Wrap all the start task command
            startTask.CommandLine = $"powershell.exe -ExecutionPolicy RemoteSigned -NoProfile \"$ErrorActionPreference='Stop'; {startTask.CommandLine}\"";

            return startTask;
        }

        private async Task AppendDeadlineSetupToStartTask(
            RenderingEnvironment environment,
            string poolName,
            StartTask startTask,
            DeadlineConfig deadlineConfig,
            InstallationPackage deadlinePackage,
            bool isWindows,
            bool useGroups)
        {
            var commandLine = startTask.CommandLine;
            var resourceFiles = new List<ResourceFile>(startTask.ResourceFiles);

            var startTaskScriptUrl = isWindows
                ? GetWindowsStartTaskUrl(environment)
                : GetLinuxStartTaskUrl(environment);

            var uri = new Uri(startTaskScriptUrl);
            var filename = uri.AbsolutePath.Split('/').Last();
            var installScriptResourceFile = new ResourceFile(httpUrl: startTaskScriptUrl, filePath: filename);
            resourceFiles.Add(installScriptResourceFile);

            commandLine += $".\\{installScriptResourceFile.FilePath}";

            if (deadlinePackage != null && !string.IsNullOrEmpty(deadlinePackage.Container))
            {
                resourceFiles.AddRange(await GetResourceFilesFromContainer(deadlinePackage.Container));
                commandLine += $" -installerPath .";
            }

            if (environment.Domain.JoinDomain)
            {
                commandLine += " -domainJoin";
                commandLine += $" -domainName {environment.Domain.DomainName}";
                commandLine += $" -domainJoinUserName {environment.Domain.DomainJoinUsername}";

                if (!string.IsNullOrWhiteSpace(environment.Domain.DomainWorkerOuPath))
                {
                    commandLine += $" -domainOuPath '{environment.Domain.DomainWorkerOuPath}'";
                }
            }

            if (environment.KeyVaultServicePrincipal != null)
            {
                commandLine += $" -tenantId {environment.KeyVaultServicePrincipal.TenantId}";
                commandLine += $" -applicationId {environment.KeyVaultServicePrincipal.ApplicationId}";
                commandLine += $" -keyVaultCertificateThumbprint {environment.KeyVaultServicePrincipal.Thumbprint}";
                commandLine += $" -keyVaultName {environment.KeyVault.Name}";
            }

            commandLine += $" -deadlineRepositoryPath {deadlineConfig.WindowsRepositoryPath}";

            if (!string.IsNullOrEmpty(deadlineConfig.RepositoryUser))
            {
                commandLine += $" -deadlineRepositoryUserName {deadlineConfig.RepositoryUser}";
            }

            if (!string.IsNullOrEmpty(deadlineConfig.ServiceUser))
            {
                commandLine += $" -deadlineServiceUserName {deadlineConfig.ServiceUser}";
            }
            else
            {
                // If the Deadline slave is running under the start task context (not a service)
                // then we don't want to wait for success as it will block after launching the
                // Deadline launcher to prevent it being killed.
                startTask.WaitForSuccess = false;
            }

            commandLine += $" -deadlineLicenseMode {deadlineConfig.LicenseMode.ToString()}";

            if (!string.IsNullOrEmpty(deadlineConfig.DeadlineRegion))
            {
                commandLine += $" -deadlineRegion {deadlineConfig.DeadlineRegion}";
            }

            if (!string.IsNullOrEmpty(deadlineConfig.LicenseServer))
            {
                commandLine += $" -deadlineLicenseServer {deadlineConfig.LicenseServer}";
            }

            if (useGroups)
            {
                commandLine += $" -deadlineGroups {poolName}";
            }
            else
            {
                commandLine += $" -deadlinePools {poolName}";
            }

            if (!string.IsNullOrWhiteSpace(deadlineConfig.ExcludeFromLimitGroups))
            {
                commandLine += $" -excludeFromLimitGroups '{deadlineConfig.ExcludeFromLimitGroups}'";
            }

            commandLine += " 2>&1;";

            startTask.CommandLine = commandLine;
            startTask.ResourceFiles = resourceFiles;
        }

        private async Task AppendQubeSetupToStartTask(
            RenderingEnvironment environment,
            string poolName,
            StartTask startTask,
            QubeConfig qubeConfig,
            InstallationPackage qubePackage,
            bool isWindows)
        {
            var commandLine = startTask.CommandLine;
            var resourceFiles = new List<ResourceFile>(startTask.ResourceFiles);

            var startTaskScriptUrl = isWindows
                ? GetWindowsStartTaskUrl(environment)
                : GetLinuxStartTaskUrl(environment);

            var uri = new Uri(startTaskScriptUrl);
            var filename = uri.AbsolutePath.Split('/').Last();
            var installScriptResourceFile = new ResourceFile(httpUrl: startTaskScriptUrl, filePath: filename);
            resourceFiles.Add(installScriptResourceFile);

            commandLine += $".\\{installScriptResourceFile.FilePath} " +
                           $"-qubeSupervisorIp {qubeConfig.SupervisorIp} " +
                           $"-workerHostGroups 'azure,{poolName}'";

            if (qubePackage != null && !string.IsNullOrEmpty(qubePackage.Container))
            {
                resourceFiles.AddRange(await GetResourceFilesFromContainer(qubePackage.Container));

                // Add qb.conf if one isn't already specified by the package
                if (!resourceFiles.Any(rf => rf.FilePath.Contains("qb.conf")))
                {
                    var qbConfResourceFile = new ResourceFile(httpUrl: _configuration["Qube:QbConf"], filePath: "qb.conf");
                    resourceFiles.Add(qbConfResourceFile);
                }
            }
            else
            {
                // No package, lets just configure
                commandLine += " -skipInstall ";
            }

            commandLine += ";";

            startTask.CommandLine = commandLine;
            startTask.ResourceFiles = resourceFiles;
        }

        private async Task AppendGpu(StartTask startTask, InstallationPackage gpuPackage)
        {
            if (gpuPackage != null)
            {
                var resourceFiles = new List<ResourceFile>(startTask.ResourceFiles);
                var gpuResourceFiles = await GetResourceFilesFromContainer(gpuPackage.Container);
                resourceFiles.AddRange(gpuResourceFiles);
                startTask.ResourceFiles = resourceFiles;
                if (!string.IsNullOrWhiteSpace(gpuPackage.PackageInstallCommand))
                {
                    var cmd = gpuPackage.PackageInstallCommand.Replace("{filename}", resourceFiles.First().FilePath);
                    startTask.CommandLine += $"{cmd}; ";
                }
            }
        }

        private async Task AppendGeneralPackages(StartTask startTask, IEnumerable<InstallationPackage> generalPackages)
        {
            if (generalPackages != null && generalPackages.Any())
            {
                foreach (var package in generalPackages)
                {
                    var resourceFiles = new List<ResourceFile>(startTask.ResourceFiles);
                    resourceFiles.AddRange(await GetResourceFilesFromContainer(package.Container));
                    startTask.ResourceFiles = resourceFiles;
                    if (!string.IsNullOrWhiteSpace(package.PackageInstallCommand))
                    {
                        startTask.CommandLine += $"{package.PackageInstallCommand}; ";
                    }
                }
            }
        }

        private void AppendDomainSetup(
            StartTask startTask,
            RenderingEnvironment environment)
        {
            if (environment.Domain != null && environment.Domain.JoinDomain)
            {
                var resourceFile = GetJoinDomainResourceFile();
                startTask.ResourceFiles.Add(resourceFile);
                startTask.CommandLine += $".\\{resourceFile.FilePath} " +
                                         $"-domainName {environment.Domain.DomainName} " +
                                         $"-domainOuPath {environment.Domain.DomainWorkerOuPath} " +
                                         $"-tenantId {environment.KeyVaultServicePrincipal.TenantId} " +
                                         $"-applicationId {environment.KeyVaultServicePrincipal.ApplicationId} " +
                                         $"-keyVaultCertificateThumbprint {environment.KeyVaultServicePrincipal.Thumbprint} " +
                                         $"-keyVaultName {environment.KeyVault.Name};";
            }
        }

        private async Task<List<ResourceFile>> GetResourceFilesFromContainer(string containerName)
        {
            var container = _blobClient.GetContainerReference(containerName);
            var resourceFiles = new List<ResourceFile>();

            var policy = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(15)),
                SharedAccessExpiryTime = DateTime.UtcNow.AddYears(1),
                Permissions = SharedAccessBlobPermissions.Read,
            };

            BlobContinuationToken blobContinuationToken = null;
            do
            {
                var results = await container.ListBlobsSegmentedAsync(null, blobContinuationToken);
                foreach (var blob in results.Results.OfType<CloudBlockBlob>())
                {
                    var sasBlobToken = blob.GetSharedAccessSignature(policy);
                    resourceFiles.Add(new ResourceFile(httpUrl: blob.Uri + sasBlobToken, filePath: blob.Name));
                }

                blobContinuationToken = results.ContinuationToken;
            } while (blobContinuationToken != null);

            return resourceFiles;
        }

        private static ResourceFile GetJoinDomainResourceFile()
        {
            var uri = new Uri(JoinDomainScript);
            var filename = uri.AbsolutePath.Split('/').Last();
            return new ResourceFile(httpUrl: JoinDomainScript, filePath: filename);
        }

        private List<EnvironmentSetting> GetEnvironmentSettings(RenderingEnvironment environment)
        {
            var envSettings = new List<EnvironmentSetting>();

            if (environment.ApplicationInsightsAccount != null)
            {
                envSettings.Add(new EnvironmentSetting("APP_INSIGHTS_APP_ID", environment.ApplicationInsightsAccount.ApplicationId));
                envSettings.Add(new EnvironmentSetting("APP_INSIGHTS_INSTRUMENTATION_KEY", environment.ApplicationInsightsAccount.InstrumentationKey));
                envSettings.Add(new EnvironmentSetting("BATCH_INSIGHTS_DOWNLOAD_URL", _configuration["BatchInsightsUrl"]));
            }

            var processesToWatch = $"{_configuration["BatchInsightsProcessesToWatch"]}";
            if (environment.AutoScaleConfiguration != null &&
                environment.AutoScaleConfiguration.SpecificProcesses != null &&
                environment.AutoScaleConfiguration.SpecificProcesses.Count > 0)
            {
                processesToWatch += $",{string.Join(',', environment.AutoScaleConfiguration.SpecificProcesses)}";
            }

            envSettings.Add(new EnvironmentSetting("AZ_BATCH_MONITOR_PROCESSES", processesToWatch));

            return envSettings;
        }
    }
}
