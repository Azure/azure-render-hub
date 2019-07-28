// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Management.Network.Models;

namespace WebApp.Models.Api
{
    /// <summary>
    /// Represents a VNetInner 
    /// </summary>
    public class AzureVirtualNetwork
    {
        [Obsolete("Only for serialization", error: true)]
        public AzureVirtualNetwork()
        { }

        public AzureVirtualNetwork(VirtualNetwork vNet)
        {
            Id = vNet.Id;
            Name = vNet.Name;
            Location = vNet.Location;
            AddressPrefixes = string.Join(',', vNet.AddressSpace.AddressPrefixes);
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string AddressPrefixes { get; set; }

        public string Location { get; set; }
    }
}
