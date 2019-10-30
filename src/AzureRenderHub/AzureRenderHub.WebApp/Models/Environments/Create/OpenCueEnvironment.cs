// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models.Environments.Create
{
    public class OpenCueEnvironment
    {
        [Required]
        public string CuebotHostnameOrIp { get; set; }

        public string Facility { get; set; }
    }
}
