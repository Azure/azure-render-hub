// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;

namespace WebApp.Config
{
    public class VirtualNetwork : AzureResource
    {
        private const string Delimiter = ";";

        public VirtualNetwork() { }

        public VirtualNetwork(string resourceIdLocationAddressPrefixes)
        {
            if (string.IsNullOrWhiteSpace(resourceIdLocationAddressPrefixes))
            {
                throw new ArgumentNullException("resourceIdLocationAddress");
            }

            var tokens = resourceIdLocationAddressPrefixes.Split(Delimiter);

            if (tokens.Length != 3)
            {
                throw new ArgumentException("Argument must be in the format ResourceId:Location:AddressPrefix", "resourceIdLocationAddress");
            }

            ResourceId = tokens[0];
            Location = tokens[1];
            AddressPrefixes = tokens[2];
        }

        public string AddressPrefixes { get; set; }

        public override string ToString()
        {
            return $"{ResourceId}{Delimiter}{Location}{Delimiter}{AddressPrefixes}";
        }
    }
}
