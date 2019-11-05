// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WebApp.Models;

namespace WebApp.Config
{
    public class InstallationPackage : ISubMenuItem
    {
        public InstallationPackage(string packageName, InstallationPackageType type)
        {
            PackageName = packageName;
            Type = type;
            Container = Guid.NewGuid().ToString();
        }

        public string PackageName { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public InstallationPackageType Type { get; set; }

        public string PackageInstallCommand { get; set; }

        public string Container { get; set; }

        public List<string> Files { get; set; } = new List<string>();

        // ISubMenuItem
        [JsonIgnore]
        public string Id => PackageName;

        [JsonIgnore]
        public string DisplayName => PackageName;

        [JsonIgnore]
        public bool Enabled => true;
    }

    public enum InstallationPackageType
    {
        Qube610,
        Qube70,
        Deadline10,
        Tractor2,
        OpenCue,
        Gpu,
        General,
    }
}
