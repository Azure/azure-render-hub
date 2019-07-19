// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Linq;
using WebApp.Config;

namespace WebApp.Models.Packages
{
    public class ViewPackageModel
    {
        public ViewPackageModel() { }

        public ViewPackageModel(InstallationPackage package)
        {
            if (package != null)
            {
                PackageName = package.PackageName;
                InstallCommandLine = package.PackageInstallCommand;
                Container = package.Container;
                Type = package.Type;

                switch (package.Type)
                {
                    case InstallationPackageType.Qube610:
                    case InstallationPackageType.Qube70:
                        InitQubePackage(package);
                        break;
                    case InstallationPackageType.Deadline10:
                    case InstallationPackageType.Tractor:
                    case InstallationPackageType.Gpu:
                    case InstallationPackageType.General:
                        InitGeneralPackage(package);
                        break;
                }
            }
        }

        private void InitQubePackage(InstallationPackage package)
        {
            PythonInstaller = package.Files.FirstOrDefault(f => f.ToLower().Contains("python"));
            QubeCoreInstaller = package.Files.FirstOrDefault(f => f.ToLower().Contains("qube-core"));
            QubeWorkerInstaller = package.Files.FirstOrDefault(f => f.ToLower().Contains("qube-worker"));
            // TODO regex for below?
            QubeJobTypes = package.Files.Where(f => f.ToLower().StartsWith("qube-") && f.ToLower().Contains("jt-")).ToList();
        }

        private void InitGeneralPackage(InstallationPackage package)
        {
            Files = package.Files == null ? "" : string.Join(", ", package.Files);
        }

        public string PackageName { get; set; }

        public string Container { get; set; }

        public InstallationPackageType Type { get; set; }

        public string InstallCommandLine { get; set; }

        public string Files { get; set; }

        // Qube
        public string PythonInstaller { get; set; }

        public string QubeCoreInstaller { get; set; }

        public string QubeWorkerInstaller { get; set; }

        public List<string> QubeJobTypes { get; set; } = new List<string>();
    }
}
