// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using WebApp.Config.Storage;

namespace WebApp.Models.Storage.Details
{
    public abstract class AssetRepositoryDetailsModel
    {
        public string Name { get; set; }

        public AssetRepositoryType RepositoryType { get; protected set; }

        public Guid SubscriptionId { get; set; }

        public string SubnetName { get; set; }

        public string SubnetVNetName { get; set; }

        public string SubnetResourceId { get; set; }

        public string SubnetLocation { get; set; }

        public string SubnetPrefix { get; set; }

        public abstract string Status { get; set; }

        public abstract string DisplayName { get; }

        public abstract string Description { get; }
    }
}
