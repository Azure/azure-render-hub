// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Filters;
using WebApp.Controllers;

namespace WebApp.Code.Attributes
{
    public class StorageActionFilterAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await base.OnActionExecutionAsync(context, next);

            if (context.Controller is StorageController controller)
            {
                controller.ViewBag.Current = "store";
                controller.ViewBag.AddRepo = true;

                if (context.ActionArguments != null && context.ActionArguments.ContainsKey("repoId"))
                {
                    // there is already a RepoId used in the view bag.
                    controller.ViewBag.StorageId = context.ActionArguments["repoId"];
                }
            }
        }
    }   
}
