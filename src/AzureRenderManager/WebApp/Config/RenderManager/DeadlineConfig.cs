// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WebApp.Code.Attributes;

namespace WebApp.Config.RenderManager
{
    public class DeadlineConfig
    {
        public string WindowsRepositoryPath { get; set; }

        public string LinuxRepositoryPath { get; set; }

        public string RepositoryUser { get; set; }

        [Credential("DeadlineShareUserPassword")]
        [JsonIgnore]
        public string RepositoryPassword { get; set; }

        // Deadline Lic server: 27008@10.2.0.6
        public string LicenseServer { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public LicenseMode LicenseMode { get; set; }

        public string DeadlineRegion { get; set; }

        public string ExcludeFromLimitGroups { get; set; }

        public bool RunAsService { get; set; }

        public string ServiceUser { get; set; }

        [Credential("DeadlineServiceUserPassword")]
        [JsonIgnore]
        public string ServicePassword { get; set; }

        [Credential("DeadlineDbClientCertificate")]
        //[JsonIgnore]
        public Certificate DeadlineDatabaseCertificate { get; set; }// = new Certificate();
    }

    public enum LicenseMode
    {
        [Description("Standard")]
        Standard,

        // We need to add support for this
//        [Description("UsageBased")]
//        UsageBased,

        [Description("LicenseFree")]
        LicenseFree
    }
}
