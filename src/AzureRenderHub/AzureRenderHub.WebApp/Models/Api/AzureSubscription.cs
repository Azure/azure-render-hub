// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Management.ResourceManager.Models;

namespace WebApp.Models.Api
{
    /// <summary>
    /// Represents a Microsoft.Azure.Management.ResourceManager.Models.Subscription
    /// for caching subscription data.
    /// </summary>
    public class AzureSubscription
    {
        public AzureSubscription(Subscription sub)
        {
            SubscriptionId = sub.SubscriptionId;
            DisplayName = sub.DisplayName;
            State = sub.State?.ToString();
        }

        [Obsolete("Only for serialization", error: true)]
        public AzureSubscription()
        {
        }
        public string SubscriptionId { get; set; }

        public string DisplayName { get; set; }

        public string State { get; set; }
    }
}
