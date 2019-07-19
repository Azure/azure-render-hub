// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using WebApp.Code.Extensions;

namespace WebApp.Identity
{
    public class IdentityProvider : IIdentityProvider
    {
        private readonly IConfiguration _configuration;

        public IdentityProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Identity GetPortalManagedServiceIdentity()
        {
            return new Identity
            {
                TenantId = Guid.Parse(_configuration["PortalManagedIdentity:TenantId"]),
                ObjectId = Guid.Parse(_configuration["PortalManagedIdentity:ObjectId"])
            };
        }

        public Identity GetCurrentUserIdentity(HttpContext context)
        {
            // We need to give the owner (the current portal user) access
            var ownerTenantId = Guid.Parse(context.User.Claims.GetTenantId());
            var ownerObjectId = Guid.Parse(context.User.Claims.GetObjectId());

            return new Identity
            {
                TenantId = ownerTenantId,
                ObjectId = ownerObjectId
            };
        }
    }
}
