// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models.Environments.Create
{
    public class TractorEnvironment
    {
        [Required]
        public string TractorSettings { get; set; }
    }
}
