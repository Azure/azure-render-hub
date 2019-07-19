// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Filters;
using WebApp.Code.Contract;

namespace WebApp.Code.Attributes
{
    public class EnvironmentsActionFilterAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await base.OnActionExecutionAsync(context, next);

            // Both Environments and Pools controllers need to list the environments for the main menu
            if (context.Controller is IEnvController controller)
            {
                controller.ViewBag.Current = "env";
                controller.ViewBag.AddEnvironment = true;

                if (context.ActionArguments != null && context.ActionArguments.ContainsKey("envId"))
                {
                    controller.ViewBag.EnvId = context.ActionArguments["envId"];
                }
            }
        }
    }   
}
