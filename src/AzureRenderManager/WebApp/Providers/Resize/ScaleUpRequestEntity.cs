// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace WebApp.Providers.Resize
{
    public class ScaleUpRequestEntity : TableEntity
    {
        public ScaleUpRequestEntity(string envName, string poolName)
        {
            PartitionKey = envName;
            RowKey = poolName;
        }

        [Obsolete("Only for serialization", error: true)]
        public ScaleUpRequestEntity() { }

        public string EnvironmentName => PartitionKey;

        public string PoolName => RowKey;

        public int TargetNodes { get; set; }
    }
}
