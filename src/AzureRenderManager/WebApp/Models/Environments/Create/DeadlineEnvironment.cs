// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using WebApp.Code;
using WebApp.Code.Attributes;
using WebApp.Config.RenderManager;

namespace WebApp.Models.Environments.Create
{
    public class DeadlineEnvironment
    {
        /// <summary>
        /// UNC share
        /// </summary>
        public string WindowsDeadlineRepositoryShare { get; set; }

        public string LinuxDeadlineRepositoryShare { get; set; }

        public string RepositoryUser { get; set; }

        [Credential("DeadlineRepositoryPassword")]
        [JsonIgnore]
        public string RepositoryPassword { get; set; }

        public bool InstallDeadlineClient { get; set; }

        // Deadline Lic server: 27008@10.2.0.6
        public string LicenseServer { get; set; }

        public LicenseMode? LicenseMode { get; set; }

        public string DeadlineRegion { get; set; }

        // Exclude these pool nodes from the following limit groups, e.g. license groups
        [RegularExpression(Validation.RegularExpressions.CommaSeparatedList, ErrorMessage = "Limit groups must be seperated by commas.")]
        public string ExcludeFromLimitGroups { get; set; }

        public bool RunAsService { get; set; }

        public string ServiceUser { get; set; }

        public string ServicePassword { get; set; }

        public bool UseDeadlineDatabaseCertificate { get; set; }

        public IFormFile DeadlineDatabaseCertificate { get; set; }

        public string DeadlineDatabaseCertificatePassword { get; set; }

        public string DeadlineDatabaseCertificateFileName { get; set; }
    }
}
