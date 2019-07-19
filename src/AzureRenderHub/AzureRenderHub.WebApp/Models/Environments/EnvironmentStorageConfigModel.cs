// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using WebApp.Arm;
using WebApp.Config;

namespace WebApp.Models.Environments
{
    public class EnvironmentStorageConfigModel : EnvironmentBaseModel
    {
        public EnvironmentStorageConfigModel()
        {
        }

        public EnvironmentStorageConfigModel(RenderManagerType renderManagerType, StorageProperties storageProps)
        {
            RenderManagerType = renderManagerType;
            AccountName = storageProps?.AccountName;
            Uri = storageProps?.Uri;
            PrimaryKey = storageProps?.PrimaryKey;
            SecondaryKey = storageProps?.SecondaryKey;
            FileShares = storageProps?.Shares;
        }

        public RenderManagerType RenderManagerType { get; set; }

        public string AccountName { get; set; }

        public Uri Uri { get; set; }

        public string PrimaryKey { get; set; }

        public string SecondaryKey { get; set; }

        public List<FileShare> FileShares { get; set; }
    }
}
