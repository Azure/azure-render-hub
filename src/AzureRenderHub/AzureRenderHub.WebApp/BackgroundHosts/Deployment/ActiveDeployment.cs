// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WebApp.BackgroundHosts.LeaseMaintainer;

namespace WebApp.BackgroundHosts.Deployment
{
    public class ActiveDeployment : ICloudQueueMessage
    {
        public DateTime StartTime { get; set; }

        public string StorageName { get; set; }

        public string Action { get; set; }

        public bool DeleteResourceGroup { get; set; }

        [JsonIgnore]
        public string QueueName { get; set; }

        [JsonIgnore]
        public string MessageId { get; set; }

        [JsonIgnore]
        public string PopReceipt { get; set; }

        [JsonIgnore]
        public int DequeueCount { get; set; }
    }
}
