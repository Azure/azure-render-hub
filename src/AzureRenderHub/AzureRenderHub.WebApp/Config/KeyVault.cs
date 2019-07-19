// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Config
{
    public class KeyVault : AzureResource
    {
        public string Uri { get; set; }
    }
}
