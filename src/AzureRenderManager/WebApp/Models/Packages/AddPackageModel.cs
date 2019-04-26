// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using WebApp.Code;
using WebApp.Config;

namespace WebApp.Models.Packages
{
    public class AddPackageModel
    {
        [Required(ErrorMessage = "Package name is a required field")]
        [RegularExpression(Validation.RegularExpressions.PackageName, ErrorMessage = Validation.Errors.Regex.PackageName)]
        public string PackageName { get; set; }

        public InstallationPackageType? Type { get; set; }

        public AddGeneralPackageModel GeneralPackage { get; set; }

        public AddQubePackageModel QubePackage { get; set; }
    }
}
