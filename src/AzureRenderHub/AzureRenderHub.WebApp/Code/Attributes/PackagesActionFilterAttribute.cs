// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Filters;
using WebApp.Controllers;

namespace WebApp.Code.Attributes
{
    public class PackagesActionFilterAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await base.OnActionExecutionAsync(context, next);

            if (context.Controller is PackagesController controller)
            {
                controller.ViewBag.Current = "pkg";
                controller.ViewBag.AddPackage = true;

                if (context.ActionArguments != null && context.ActionArguments.ContainsKey("pkgId"))
                {
                    controller.ViewBag.PkgId = context.ActionArguments["pkgId"];
                }
            }
        }
    }   
}
