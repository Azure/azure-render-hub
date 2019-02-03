// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebApp.Code.Attributes;

namespace WebApp.Config.RenderManager
{
    public class Certificate
    {
        public byte[] CertificateData { get; set; }

        [Credential("DeadlineDbClientCertificatePassword")]
        [JsonIgnore]
        public string Password { get; set; }
    }
}
