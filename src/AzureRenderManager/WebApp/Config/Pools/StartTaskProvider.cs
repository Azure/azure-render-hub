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
            IEnumerable<string> additionalGroups,
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
                GetEnvironmentSettings(environment, isWindows),
                new UserIdentity(
                    autoUser: new AutoUserSpecification(AutoUserScope.Pool, ElevationLevel.Admin)),
                3, // retries
                true); // waitForSuccess

            AppendGpu(startTask, gpuPackage);

            AppendDomainSetup(startTask, environment);

            AppendGeneralPackages(startTask, generalPackages);

            await AppendQubeSetupToStartTask(
                environment,
                poolName,
                additionalGroups,
                startTask,
                environment.RenderManagerConfig.Qube,
                qubePackage,
                isWindows);

            // Wrap all the start task command
            startTask.CommandLine = $"powershell.exe -ExecutionPolicy RemoteSigned -NoProfile \"$ErrorActionPreference='Stop'; {startTask.CommandLine}\"";

            return startTask;
        }

        public async Task<StartTask> GetDeadlineStartTask(
            string poolName,
            IEnumerable<string> additionalPools,
            IEnumerable<string> additionalGroups,
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
                GetEnvironmentSettings(environment, isWindows),
                new UserIdentity(
                    autoUser: new AutoUserSpecification(AutoUserScope.Pool, ElevationLevel.Admin)),
                3, // retries
                true); // waitForSuccess

            AppendGpu(startTask, gpuPackage);
            AppendGeneralPackages(startTask, generalPackages);

            AppendDeadlineSetupToStartTask(
                environment,
                poolName,
                additionalPools,
                additionalGroups,
                startTask,
                environment.RenderManagerConfig.Deadline,
                deadlinePackage,
                isWindows,
                useGroups);

            // Wrap all the start task command
            if (isWindows)
            {
                startTask.CommandLine = $"powershell.exe -ExecutionPolicy RemoteSigned -NoProfile \"$ErrorActionPreference='Stop'; {startTask.CommandLine}\"";
            }
            else
            {
                startTask.CommandLine = $"/bin/bash -c 'set -e; set -o pipefail; {startTask.CommandLine}'";
            }
            
            return startTask;
        }

        private void AppendDeadlineSetupToStartTask(
            RenderingEnvironment environment,
            string poolName,
            IEnumerable<string> additionalPools,
            IEnumerable<string> additionalGroups,
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

            if (isWindows)
            {
                commandLine += $".\\{installScriptResourceFile.FilePath} ";
            }
            else
            {
                commandLine += $"./{installScriptResourceFile.FilePath} ";
            }

            if (deadlinePackage != null && !string.IsNullOrEmpty(deadlinePackage.Container))
            {
                resourceFiles.Add(GetContainerResourceFile(deadlinePackage.Container, deadlinePackage.PackageName));
                commandLine += GetParameterSet(isWindows, "installerPath", deadlinePackage.PackageName);
            }

            if (isWindows & environment.Domain != null && environment.Domain.JoinDomain)
            {
                commandLine += GetParameterSet(isWindows, "domainJoin");
                commandLine += GetParameterSet(isWindows, "domainName", environment.Domain.DomainName);
                commandLine += GetParameterSet(isWindows, "domainJoinUserName", environment.Domain.DomainJoinUsername);

                if (!string.IsNullOrWhiteSpace(environment.Domain.DomainWorkerOuPath))
                {
                    commandLine += GetParameterSet(isWindows, "domainOuPath", environment.Domain.DomainWorkerOuPath);
                }
            }

            if (environment.KeyVaultServicePrincipal != null)
            {
                commandLine += GetParameterSet(isWindows, "tenantId", environment.KeyVaultServicePrincipal.TenantId.ToString());
                commandLine += GetParameterSet(isWindows, "applicationId", environment.KeyVaultServicePrincipal.ApplicationId.ToString());
                commandLine += GetParameterSet(isWindows, "keyVaultCertificateThumbprint", environment.KeyVaultServicePrincipal.Thumbprint);
                commandLine += GetParameterSet(isWindows, "keyVaultName", environment.KeyVault.Name);
            }

            var repoPath = isWindows ? deadlineConfig.WindowsRepositoryPath : deadlineConfig.LinuxRepositoryPath;
            if (!string.IsNullOrWhiteSpace(repoPath))
            {
                commandLine += GetParameterSet(isWindows, "deadlineRepositoryPath", deadlineConfig.WindowsRepositoryPath);
            }

            if (!string.IsNullOrEmpty(deadlineConfig.RepositoryUser))
            {
                commandLine += GetParameterSet(isWindows, "deadlineRepositoryUserName", deadlineConfig.RepositoryUser);
            }

            if (!string.IsNullOrEmpty(deadlineConfig.ServiceUser))
            {
                commandLine += GetParameterSet(isWindows, "deadlineServiceUserName", deadlineConfig.ServiceUser);
            }
            else
            {
                // If the Deadline slave is running under the start task context (not a service)
                // then we don't want to wait for success as it will block after launching the
                // Deadline launcher to prevent it being killed.
                startTask.WaitForSuccess = false;
            }

            if (deadlineConfig.LicenseMode != null)
            {
                commandLine += GetParameterSet(isWindows, "deadlineLicenseMode", deadlineConfig.LicenseMode.ToString());
            }
            
            if (!string.IsNullOrEmpty(deadlineConfig.DeadlineRegion))
            {
                commandLine += GetParameterSet(isWindows, "deadlineRegion", deadlineConfig.DeadlineRegion);
            }

            if (!string.IsNullOrEmpty(deadlineConfig.LicenseServer))
            {
                commandLine += GetParameterSet(isWindows, "deadlineLicenseServer", deadlineConfig.LicenseServer);
            }

            var pools = useGroups ? "" : poolName;
            if (additionalPools != null && additionalPools.Any())
            {
                pools += string.IsNullOrEmpty(pools) ? "" : ",";
                pools += string.Join(',', additionalPools);
            }

            if (!string.IsNullOrEmpty(pools))
            {
                commandLine += GetParameterSet(isWindows, "deadlinePools", pools);
            }

            var groups = useGroups ? poolName : "";
            if (additionalGroups != null && additionalGroups.Any())
            {
                groups += string.IsNullOrEmpty(groups) ? "" : ",";
                groups += string.Join(',', additionalGroups);
            }

            if (!string.IsNullOrEmpty(groups))
            {
                commandLine += GetParameterSet(isWindows, "deadlineGroups", groups);
            }

            if (!string.IsNullOrWhiteSpace(deadlineConfig.ExcludeFromLimitGroups))
            {
                commandLine += GetParameterSet(isWindows, "excludeFromLimitGroups", deadlineConfig.ExcludeFromLimitGroups);
            }

            commandLine += "; ";

            if (!isWindows)
            {
                commandLine += "wait";
            }

            startTask.CommandLine = commandLine;
            startTask.ResourceFiles = resourceFiles;
        }

        private string GetParameterSet(bool isWindows, string parameterName, string parameterValue = null)
        {
            var param = $"{parameterName}";
            if (parameterValue != null)
            {
                param += $" '{parameterValue}'";
            }

            // Leave the trailing space below!
            return isWindows ? $"-{param}" : $"--{param} ";
        }

        private async Task AppendQubeSetupToStartTask(
            RenderingEnvironment environment,
            string poolName,
            IEnumerable<string> additionalGroups,
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

            var workerGroups = $"azure,{poolName}";
            if (additionalGroups != null && additionalGroups.Any())
            {
                workerGroups += $",{string.Join(',', additionalGroups)}";
            }

            commandLine += $".\\{installScriptResourceFile.FilePath} " +
                           $"-qubeSupervisorIp {qubeConfig.SupervisorIp} " +
                           $"-workerHostGroups '{workerGroups}'";

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

        private void AppendGpu(StartTask startTask, InstallationPackage gpuPackage)
        {
            if (gpuPackage != null)
            {
                var resourceFile = GetContainerResourceFile(gpuPackage.Container, gpuPackage.PackageName);
                startTask.ResourceFiles = new List<ResourceFile>(startTask.ResourceFiles) { resourceFile };

                if (!string.IsNullOrWhiteSpace(gpuPackage.PackageInstallCommand))
                {
                    startTask.CommandLine += $"pushd {gpuPackage.PackageName}; {gpuPackage.PackageInstallCommand}; popd; ";
                }
            }
        }

        private void AppendGeneralPackages(StartTask startTask, IEnumerable<InstallationPackage> generalPackages)
        {
            if (generalPackages != null)
            {
                foreach (var package in generalPackages)
                {
                    var resourceFile = GetContainerResourceFile(package.Container, package.PackageName);
                    startTask.ResourceFiles = new List<ResourceFile>(startTask.ResourceFiles) { resourceFile };

                    if (!string.IsNullOrWhiteSpace(package.PackageInstallCommand))
                    {
                        startTask.CommandLine += $"pushd {package.PackageName}; {package.PackageInstallCommand}; popd; ";
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

        private ResourceFile GetContainerResourceFile(string containerName, string filePath)
        {
            var container = _blobClient.GetContainerReference(containerName);
            var policy = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(15)),
                SharedAccessExpiryTime = DateTime.UtcNow.AddYears(1),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List,
            };

            var containerSas = container.GetSharedAccessSignature(policy);
            return new ResourceFile(storageContainerUrl: container.Uri + containerSas, filePath: filePath);
        }

        private static ResourceFile GetJoinDomainResourceFile()
        {
            var uri = new Uri(JoinDomainScript);
            var filename = uri.AbsolutePath.Split('/').Last();
            return new ResourceFile(httpUrl: JoinDomainScript, filePath: filename);
        }

        private List<EnvironmentSetting> GetEnvironmentSettings(RenderingEnvironment environment, bool isWindows)
        {
            var envSettings = new List<EnvironmentSetting>();

            if (environment.ApplicationInsightsAccount != null)
            {
                envSettings.Add(new EnvironmentSetting("APP_INSIGHTS_APP_ID", environment.ApplicationInsightsAccount.ApplicationId));
                envSettings.Add(new EnvironmentSetting("APP_INSIGHTS_INSTRUMENTATION_KEY", environment.ApplicationInsightsAccount.InstrumentationKey));

                var batchInsightsUrl = isWindows ? _configuration["BatchInsightsWindowsUrl"] : _configuration["BatchInsightsLinuxUrl"];
                envSettings.Add(new EnvironmentSetting("BATCH_INSIGHTS_DOWNLOAD_URL", batchInsightsUrl));

                var processesToWatch = isWindows ? _configuration["BatchInsightsWindowsProcessesToWatch"] : _configuration["BatchInsightsLinuxProcessesToWatch"];
                if (environment.AutoScaleConfiguration != null &&
                    environment.AutoScaleConfiguration.SpecificProcesses != null &&
                    environment.AutoScaleConfiguration.SpecificProcesses.Count > 0)
                {
                    processesToWatch += $",{string.Join(',', environment.AutoScaleConfiguration.SpecificProcesses)}";
                }

                if (!string.IsNullOrWhiteSpace(processesToWatch))
                {
                    envSettings.Add(new EnvironmentSetting("AZ_BATCH_MONITOR_PROCESSES", processesToWatch));
                }
            }

            return envSettings;
        }
    }
}
