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
            var squishedCosts = usages.Where(u => u.Cost != null).Select(u => u.Cost.Recategorize(u.EnvironmentId));

            From = from;
            To = to;
            SummaryUsage = CalculateSummarySafely(squishedCosts);
            UsagePerEnvironment = usages.OrderBy(eu => eu.EnvironmentId).ToList();
            NextMonthLink = nextMonth;
            CurrentMonthLink = currentMonth;
            PreviousMonthLink = prevMonth;
        }

        // Returns a summary, if possible.  Some circumstances, like different currencies,
        // prevent us creating a summary.
        private Cost CalculateSummarySafely(IEnumerable<Cost> squishedCosts)
        {
            try
            {
                return squishedCosts.Any() ? squishedCosts.Aggregate(Cost.Aggregate) : null;
            }
            catch (Exception e)
            {
                return null;
            }
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
