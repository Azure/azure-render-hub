// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebApp.Identity
{
    public interface IIdentityProvider
    {
        Identity GetPortalManagedServiceIdentity();
        Identity GetCurrentUserIdentity(HttpContext context);
    }
}
