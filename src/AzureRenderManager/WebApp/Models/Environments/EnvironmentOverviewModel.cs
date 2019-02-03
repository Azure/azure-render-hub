// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Models.Environments.Details;

namespace WebApp.Models.Environments
{
    public class EnvironmentOverviewModel
    {
        public List<ViewEnvironmentModel> Environments { get; set; } = new List<ViewEnvironmentModel>();
    }
}
