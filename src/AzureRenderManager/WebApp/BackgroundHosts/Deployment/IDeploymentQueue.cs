// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApp.BackgroundHosts.Deployment
{
    public interface IDeploymentQueue
    {
        Task<IEnumerable<ActiveDeployment>> Get();
        Task Add(ActiveDeployment activeDeployment);
        Task Delete(string messageId, string popReceipt);
    }
}
