// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Rest;
using WebApp.Config;

namespace WebApp.Arm
{
    public interface IAccessTokenProvider
    {
        Task<ServiceClientCredentials> GetCredentials(RenderingEnvironment env);

        ClaimsPrincipal GetUser();
    }
}
