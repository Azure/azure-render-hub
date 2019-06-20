// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Code;
using WebApp.Config;

namespace WebApp.Models.Pools
{
    public class PoolConfigurationModel : PoolBaseModel
    {
        public RenderManagerType RenderManagerType { get; set; }

        [Required]
        [StringLength(64)]
        [RegularExpression(Validation.RegularExpressions.PoolName, ErrorMessage = Validation.Errors.Regex.PoolName)]
        public string PoolName { get; set; }

        [Required]
        public string SelectedRenderManagerPackageId { get; set; }

        [Required]
        public string SelectedGpuPackageId { get; set; }

        public string[] SelectedGeneralPackageIds { get; set; }

        public string ImageReference { get; set; }

        public List<SelectListItem> RenderManagerPackages { get; } = new List<SelectListItem>();

        public List<SelectListItem> GpuPackages { get; } = new List<SelectListItem>();

        public List<SelectListItem> GeneralPackages { get; } = new List<SelectListItem>();

        public List<SelectListItem> OfficialImageReferences { get; } = new List<SelectListItem>();

        public string CustomImageReference { get; set; }

        public List<SelectListItem> CustomImageReferences { get; } = new List<SelectListItem>();

        public bool MaxAppLicense { get; set; }

        public bool MayaAppLicense { get; set; }

        public bool ArnoldAppLicense { get; set; }

        public bool VrayAppLicense { get; set; }

        public List<SelectListItem> Sizes { get; } = new List<SelectListItem>();

        [Required]
        [Range(0, 10_000)]
        public int DedicatedNodes { get; set; }

        [Required]
        [Range(0, 10_000)]
        public int LowPriorityNodes { get; set; }

        public List<SelectListItem> AutoScalePolicyItems { get; } = new List<SelectListItem>();

        [Required]
        public string VmSize { get; set; }

        // By default we add all compute nodes to a Deadline 'pool' with the same name as the Batch pool name,
        // this behavior can be ovverriden to use Deadline groups instead
        public bool UseDeadlineGroups { get; set; }
    }
}
