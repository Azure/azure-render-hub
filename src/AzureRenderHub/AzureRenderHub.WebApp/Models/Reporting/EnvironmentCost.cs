// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WebApp.Models.Reporting
{
    public class EnvironmentCost
    {
        public EnvironmentCost(string envId, Cost cost)
        {
            EnvironmentId = envId;
            Cost = cost;
        }

        public string EnvironmentId { get; }

        public string NextMonthLink { get; set;  }

        public string CurrentMonthLink { get; set;  }

        public string PreviousMonthLink { get; set;  }

        public Cost Cost { get; }
    }
}
