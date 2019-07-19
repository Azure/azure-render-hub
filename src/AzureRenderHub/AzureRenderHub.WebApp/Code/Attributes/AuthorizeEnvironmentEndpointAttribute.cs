// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApp.Code.Contract;

namespace WebApp.Code.Attributes
{
    public class AuthorizeEnvironmentEndpointAttribute : TypeFilterAttribute
    {
        public AuthorizeEnvironmentEndpointAttribute()
            : base(typeof(AuthorizeEnvironmentEndpointFilter))
        {
        }
    }


    public class AuthorizeEnvironmentEndpointFilter : IAsyncAuthorizationFilter
    {
        private readonly IEnvironmentCoordinator _environmentCoordinator;

        public AuthorizeEnvironmentEndpointFilter(IEnvironmentCoordinator environmentCoordinator)
        {
            _environmentCoordinator = environmentCoordinator;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context.HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = context.HttpContext.Request.Headers["Authorization"];

                if (context.RouteData.Values.ContainsKey("environmentName") &&
                    context.RouteData.Values["environmentName"] is string envName)
                {
                    if (!string.IsNullOrEmpty(envName))
                    {
                        var env = await _environmentCoordinator.GetEnvironment(envName);
                        if (env != null &&
                            env.Enabled &&
                            env.AutoScaleConfiguration != null &&
                            env.AutoScaleConfiguration.ScaleEndpointEnabled)
                        {
                            if (!string.IsNullOrWhiteSpace(env.AutoScaleConfiguration.PrimaryApiKey) &&
                                !string.IsNullOrWhiteSpace(env.AutoScaleConfiguration.SecondaryApiKey))
                            {
                                if (authHeader == $"Basic {env.AutoScaleConfiguration.PrimaryApiKey}" ||
                                    authHeader == $"Basic {env.AutoScaleConfiguration.SecondaryApiKey}")
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            context.Result = new StatusCodeResult((int)HttpStatusCode.Forbidden);
        }
    }
}