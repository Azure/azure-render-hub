// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Http;
using WebApp.Code;

namespace WebApp.Models.Packages
{
    public class AddQubePackageModel
    {
        //[Required(ErrorMessage = "Package name is a required field")]
        //[RegularExpression(Validation.RegularExpressions.PackageName, ErrorMessage = Validation.Errors.Regex.PackageName)]
        //public string PackageName { get; set; }

        public string InstallCommandLine { get; set; }

        public IFormFile QbConf { get; set; }

        //[Required]
        public IFormFile PythonInstaller { get; set; }

        //[Required]
        public IFormFile QubeCoreMsi { get; set; }

        //[Required]
        public IFormFile QubeWorkerMsi { get; set; }

        public IEnumerable<IFormFile> QubeJobTypeMsis { get; set; }
    }
}
