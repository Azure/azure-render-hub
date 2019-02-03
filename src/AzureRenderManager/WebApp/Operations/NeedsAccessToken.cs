// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Rest;

namespace WebApp.Operations
{
    public abstract class NeedsAccessToken
    {
        private readonly IHttpContextAccessor _contextAccessor;

        protected NeedsAccessToken(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public async Task<string> GetAccessToken()
        {
            var accessToken = await _contextAccessor.HttpContext.GetTokenAsync("access_token");
            return accessToken;
        }

        public ClaimsPrincipal GetUser() => _contextAccessor.HttpContext.User;
    }
}