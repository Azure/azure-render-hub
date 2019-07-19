// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using WebApp.Models.Environments;

namespace WebApp.Models.Pools
{
    public class PoolListModel : EnvironmentBaseModel
    {
        public string BatchAccount { get; set; }

        public string Location { get; set; }

        public List<PoolListDetailsModel> Pools { get; } = new List<PoolListDetailsModel>();
    }
}
