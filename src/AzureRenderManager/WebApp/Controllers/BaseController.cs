// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    [Authorize]
    [RequireHttps]
    [AutoValidateAntiforgeryToken]
    public class BaseController : Controller
    {
        protected Task<string> GetAccessToken() => HttpContext.GetTokenAsync("access_token");
    }
}
