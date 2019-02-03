// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebApp.BackgroundHosts.LeaseMaintainer
{
    public interface ILeaseMaintainer
    {
        Task MaintainLease(ICloudQueueMessage message, CancellationToken ct);
    }
}
