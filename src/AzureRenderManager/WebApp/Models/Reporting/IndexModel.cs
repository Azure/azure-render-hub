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
            EnvironmentUsage[] usages,
            string nextMonth,
            string currentMonth,
            string prevMonth)
        {
            From = from;
            To = to;
            SummaryUsage = usages.Select(x => x.Usage).Aggregate((x, y) => new Usage(x, y));
            UsagePerEnvironment = usages.OrderBy(eu => eu.EnvironmentId).ToList();
            NextMonthLink = nextMonth;
            CurrentMonthLink = currentMonth;
            PreviousMonthLink = prevMonth;
        }

        public DateTimeOffset From { get; }

        public DateTimeOffset To { get; }

        public Usage SummaryUsage { get; }

        public IReadOnlyList<EnvironmentUsage> UsagePerEnvironment { get; }

        public string NextMonthLink { get; }

        public string CurrentMonthLink { get; }

        public string PreviousMonthLink { get; }

    }

}
