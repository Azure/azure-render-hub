// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;
using WebApp.Code.Attributes;

namespace WebApp.Config
{
    public class ServicePrincipal
    {
        public string Name { get; set; }

        public Guid TenantId { get; set; }

        public Guid ApplicationId { get; set; }

        public Guid ObjectId { get; set; }

        [Credential("ServicePrincipalPassword")]
        [JsonIgnore]
        public string Password { get; set; }

        public string Thumbprint { get; set; }

        public string CertificateKeyVaultName { get; set; }
    }
}
