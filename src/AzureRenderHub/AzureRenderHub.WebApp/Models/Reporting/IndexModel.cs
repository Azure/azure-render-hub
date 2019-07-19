// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApp.Models.Reporting
{
    public class IndexModel
    {
        public IndexModel(
            DateTimeOffset from,
            DateTimeOffset to,
            EnvironmentCost[] usages,
            string nextMonth,
            string currentMonth,
            string prevMonth)
        {
            var costs = usages.Select(x => x.Cost).Where(c => c != null).ToList();

            From = from;
            To = to;
            SummaryUsage = costs.Any() ? costs.Aggregate((x, y) => new Cost(x, y)) : null;
            UsagePerEnvironment = usages.OrderBy(eu => eu.EnvironmentId).ToList();
            NextMonthLink = nextMonth;
            CurrentMonthLink = currentMonth;
            PreviousMonthLink = prevMonth;
        }

        public DateTimeOffset From { get; }

        public DateTimeOffset To { get; }

        public Cost SummaryUsage { get; }

        public IReadOnlyList<EnvironmentCost> UsagePerEnvironment { get; }

        public string NextMonthLink { get; }

        public string CurrentMonthLink { get; }

        public string PreviousMonthLink { get; }

    }

}
