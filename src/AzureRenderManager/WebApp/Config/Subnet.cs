// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WebApp.Config
{
    public class Subnet : AzureResource
    {
        public string VnetResourceId { get; set; }

        public string AddressPrefix { get; set; }

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
            return $"{ResourceId};{Location};{AddressPrefix}";
        }
    }
}
