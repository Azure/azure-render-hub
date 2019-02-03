// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Filters;
using WebApp.Controllers;

namespace WebApp.Code.Attributes
{
    public class MenuActionFilterAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await base.OnActionExecutionAsync(context, next);

            if (context.Controller is MenuBaseController controller)
            {
                // set these on here after the action so we can display them in the sub menu
                // these are cached so shouldn't be too much extra wait time.
                controller.ViewBag.Packages = await controller.Packages();
                controller.ViewBag.Environments = await controller.Environments();
                controller.ViewBag.Repositories = await controller.Repositories();
            }
        }
    }   
}
