// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WebApp.BackgroundHosts.LeaseMaintainer
{
    public interface ICloudQueueMessage
    {
        string QueueName { get; set; }

        string MessageId { get; set; }

        string PopReceipt { get; set; }
    }
}
