// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Newtonsoft.Json;
using WebApp.Code.Attributes;

namespace WebApp.Config
{
    public class DomainConfig
    {
        public bool JoinDomain { get; set; }

        public string DomainName { get; set; }

        public string DomainWorkerOuPath { get; set; }

        // A domain user that has permissions to join computers to the domain
        public string DomainJoinUsername { get; set; }

        [Credential("DomainJoinUserPassword")]
        [JsonIgnore]
        public string DomainJoinPassword { get; set; }
    }
}
