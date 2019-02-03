// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Arm
{
    public class FileShare
    {
        public Uri Uri { get; set; }

        public string ShareName { get; set; }

        public int? Quota { get; set; }
    }
}
