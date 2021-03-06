﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WebApp.Config.Storage
{
    /// <summary>
    /// Source share to replicate
    /// </summary>
    public class NfsFileShare
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public long Size { get; set; }

        public long Used { get; set; }

        public int Disks { get; set; }
    }
}
