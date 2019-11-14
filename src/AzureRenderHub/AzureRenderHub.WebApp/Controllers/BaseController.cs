// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Client;

namespace WebApp.Controllers
{
    [Authorize]
    [RequireHttps]
    [AutoValidateAntiforgeryToken]
    public class BaseController : Controller
    {
        private readonly ITokenAcquisition _tokenAcquisition;

        public BaseController(ITokenAcquisition tokenAcquisition = null)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        protected async Task<string> GetAccessToken(string scope = "https://management.azure.com/user_impersonation")
        {
            return await _tokenAcquisition.GetAccessTokenOnBehalfOfUser(
                HttpContext,
                new[] { scope });
        }
    }
}
