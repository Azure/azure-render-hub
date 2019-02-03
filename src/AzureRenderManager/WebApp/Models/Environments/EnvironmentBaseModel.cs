// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using WebApp.Code;
using WebApp.Code.Extensions;
using WebApp.Config;

namespace WebApp.Models.Environments
{
    public class EnvironmentBaseModel
    {
        [Required(ErrorMessage = "Environment name is a required field")]
        [RegularExpression(Validation.RegularExpressions.EnvironmentName, ErrorMessage = Validation.Errors.Regex.EnvironmentName)]
        [StringLength(64)]
        public string EnvironmentName { get; set; }

        public bool EditMode { get; set; }

        public RenderManagerType? RenderManager { get; set; }

        public string RenderManagerName => RenderManager.HasValue ? RenderManager.GetDescription() : "Render Manager";
    }
}
