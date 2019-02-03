// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Code.Contract;
using WebApp.Config;

namespace WebApp.Code.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireConfigAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var config = context.HttpContext.RequestServices.GetService<IConfiguration>();
            var ignoreMissingConfig = config.GetValue<bool>("IgnoreMissingConfig");

            var controllerName = context.RouteData.Values["controller"].ToString();
            var actionName = context.RouteData.Values["action"].ToString();

            var ignorableRoute = controllerName == "Environments" && (actionName.StartsWith("New") || actionName.StartsWith("Step1"));


            if (ignoreMissingConfig || ignorableRoute)
            {
                await base.OnActionExecutionAsync(context, next);
            }
            else
            {
                var environmentCoordinator = context.HttpContext.RequestServices.GetService<IEnvironmentCoordinator>();
                var envs = await environmentCoordinator.ListEnvironments();
                if (envs != null && envs.Any())
                {
                    await base.OnActionExecutionAsync(context, next);
                }
                else
                {
                    // No environments, redirect to wizard
                    context.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Environments", action = "Step1" }));
                }
            }
        }
    }
}