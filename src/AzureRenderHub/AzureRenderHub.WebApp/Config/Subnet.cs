// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace WebApp.Config
{
    public class Subnet : AzureResource
    {
        private const string Delimiter = ";";

        public Subnet() { }

        public Subnet(string resourceIdLocationAddress)
        {
            if (string.IsNullOrWhiteSpace(resourceIdLocationAddress))
            {
                throw new ArgumentNullException("resourceIdLocationAddress");
            }

            var tokens = resourceIdLocationAddress.Split(Delimiter);

            if (tokens.Length != 3)
            {
                throw new ArgumentException("Argument must be in the format ResourceId:Location:AddressPrefix", "resourceIdLocationAddress");
            }

            ResourceId = tokens[0];
            Location = tokens[1];
            AddressPrefix = tokens[2];
        }

        public string VnetResourceId {
            get
            {
                if (string.IsNullOrEmpty(ResourceId))
                {
                    return null;
                }

                var tokens = ResourceId.Split("/subnets/");

                if (tokens.Length != 2)
                {
                    return null;
                }

                return tokens[0]; //VNet Resource Id
            }
        }

        public string AddressPrefix { get; set; }

        public string VNetAddressPrefixes { get; set; }

        public string VNetName
        {
            get
            {
                if (string.IsNullOrEmpty(ResourceId))
                {
                    return null;
                }

                var tokens = ResourceId.Split("/");

                if (tokens.Length != 11)
                {
                    return null;
                }

                return tokens[8]; //VNet name
            }
        }

        public override string ToString()
        {
            return $"{ResourceId}{Delimiter}{Location}{Delimiter}{AddressPrefix}";
        }
    }
}
