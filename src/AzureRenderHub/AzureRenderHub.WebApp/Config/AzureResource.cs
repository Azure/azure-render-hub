// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Config
{
    public class AzureResource
    {
        public string ResourceId { get; set; }

        public string Location { get; set; }

        public string ResourceGroupName
        {
            get
            {
                if (string.IsNullOrEmpty(ResourceId))
                {
                    return null;
                }

                var tokens = ResourceId.Split("/");
                if (tokens.Length > 4)
                {
                    return tokens[4]; //Resource group
                }

                return null;
            }
        }

        public Guid SubscriptionId
        {
            get
            {
                if (string.IsNullOrEmpty(ResourceId))
                {
                    return Guid.Empty;
                }
                var tokens = ResourceId.Split("/", StringSplitOptions.RemoveEmptyEntries);
                return Guid.Parse(tokens[1]);
            }
        }

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(ResourceId))
                {
                    return null;
                }

                return ResourceId.Split("/").LastOrDefault();
            }
        }

        public bool ExistingResource { get; set;  }
    }
}
