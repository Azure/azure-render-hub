// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models.Api
{
    public class ScaleUpRequest
    {
        // TODO: Handle these in a future version: 

        //public int? TargetDedicatedNodes { get; set; }
        //public int? TargetLowPriorityNodes { get; set; }

        [JsonProperty(Required = Required.Always)]
        [Range(minimum: 0, maximum: int.MaxValue, ErrorMessage = "requestedNodes must be a non-negative number")]
        public int RequestedNodes { get; set; }
    }
}
