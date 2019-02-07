// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WebApp.Models.Packages
{
    public class AddPackageModel {

        public string PackageName { get; set; }

        public AddGeneralPackageModel GeneralPackage { get; set; }

        public AddQubePackageModel QubePackage { get; set; }
    }
}
