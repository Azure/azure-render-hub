// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web.Client;
using Microsoft.Rest;

namespace WebApp.Operations
{
    public abstract class NeedsAccessToken
    {
        protected readonly IHttpContextAccessor _contextAccessor;
        private readonly ITokenAcquisition _tokenAcquisition;

        protected NeedsAccessToken(
            IHttpContextAccessor contextAccessor,
            ITokenAcquisition tokenAcquisition)
        {
            _contextAccessor = contextAccessor;
            _tokenAcquisition = tokenAcquisition;
        }

        public async Task<string> GetAccessToken(string scope = "https://management.azure.com/user_impersonation")
        {
            return await _tokenAcquisition.GetAccessTokenOnBehalfOfUser(
                _contextAccessor.HttpContext,
                new[] { scope });
        }

        public ClaimsPrincipal GetUser() => _contextAccessor.HttpContext.User;
    }
}