// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WebApp.Config
{
    public class BatchAccount : AzureResource
    {
        public string Url { get; set; }

        public string GetCertificateId(string thumbprint)
        {
            return $"{ResourceId}/certificates/SHA1-{thumbprint.ToUpper()}";
        }
    }
}
