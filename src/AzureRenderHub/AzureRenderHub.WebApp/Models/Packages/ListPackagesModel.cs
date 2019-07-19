// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using WebApp.Config;

namespace WebApp.Models.Packages
{
    public class ListPackagesModel
    {
        public List<InstallationPackage> Packages { get; set; }
    }
}
