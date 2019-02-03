// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Azure.Management.Compute.Models;
using WebApp.Config;

namespace WebApp.Operations
{
    public interface IVMSizes
    {
        Task<IReadOnlyList<VirtualMachineSize>> GetSizes(RenderingEnvironment envId);

        // yeah, returning view stuff from here, sorry
        Task<IReadOnlyList<SelectListItem>> GetSizesSelectList(RenderingEnvironment envId);
    }
}