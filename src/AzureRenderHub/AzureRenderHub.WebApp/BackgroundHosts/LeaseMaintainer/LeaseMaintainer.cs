// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;

namespace WebApp.BackgroundHosts.LeaseMaintainer
{
    public class LeaseMaintainer : ILeaseMaintainer
    {
        private readonly CloudQueueClient _queueClient;

        public LeaseMaintainer(CloudQueueClient queueClient)
        {
            _queueClient = queueClient;
        }

        public async Task MaintainLease(ICloudQueueMessage message, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var queue = _queueClient.GetQueueReference(message.QueueName);
                    if (await queue.ExistsAsync())
                    {
                        var msg = new CloudQueueMessage(message.MessageId, message.PopReceipt);
                        await queue.UpdateMessageAsync(msg, TimeSpan.FromSeconds(60), MessageUpdateFields.Visibility);
                        message.MessageId = msg.Id;
                        message.PopReceipt = msg.PopReceipt;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception renewing message lease: {e}");
                }

                Thread.Sleep(TimeSpan.FromSeconds(25));
            }
        }
    }
}
