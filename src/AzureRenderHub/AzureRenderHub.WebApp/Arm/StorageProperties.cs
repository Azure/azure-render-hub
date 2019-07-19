// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Arm
{
    public class StorageProperties
    {
        public string AccountName { get; set; }

        public Uri Uri { get; set; }

        public string PrimaryKey { get; set; }

        public string SecondaryKey { get; set; }

        public List<FileShare> Shares { get; set; } = new List<FileShare>();
    }
}
