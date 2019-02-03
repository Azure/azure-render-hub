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
        public string FileServerName { get; set; }
        public string Action { get; set; }

        // For delete
        public string AvSetName { get; set; }
        public string NicName { get; set; }
        public string PipName { get; set; }
        public string NsgName { get; set; }
        public string OsDiskName { get; set; }
        public List<string> DataDiskNames { get; set; }

        [JsonIgnore]
        public string QueueName { get; set; }

        [JsonIgnore]
        public string MessageId { get; set; }

        [JsonIgnore]
        public string PopReceipt { get; set; }
    }
}
