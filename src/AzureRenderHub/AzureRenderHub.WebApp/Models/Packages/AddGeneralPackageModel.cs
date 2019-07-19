// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Http;
using WebApp.Code;

namespace WebApp.Models.Packages
{
    public class AddGeneralPackageModel
    {
        //[Required(ErrorMessage = "Package name is a required field")]
        //[RegularExpression(Validation.RegularExpressions.PackageName, ErrorMessage = Validation.Errors.Regex.PackageName)]
        //public string PackageName { get; set; }

        public string InstallCommandLine { get; set; }

        //[Required]
        public IEnumerable<IFormFile> Files { get; set; }
    }
}
