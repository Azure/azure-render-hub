// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Management.Network.Models;

namespace WebApp.Models.Api
{
    /// <summary>
    /// Represents a SubnetInner that contains it's vNet parents name and location.
    /// </summary>
    public class AzureSubnet
    {
        [Obsolete("Only for serialization", error: true)]
        public AzureSubnet()
        { }

        public AzureSubnet(VirtualNetwork vNet, Subnet inner)
        {
            Id = inner.Id;
            AddressPrefix = inner.AddressPrefix;
            Name = inner.Name;
            VNetName = vNet.Name;
            VNetAddressPrefixes = string.Join(',', vNet.AddressSpace.AddressPrefixes);
            Location = vNet.Location;
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string AddressPrefix { get; set; }

        public string VNetName { get; set; }

        public string VNetAddressPrefixes { get; set; }

        public string Location { get; set; }
    }
}
