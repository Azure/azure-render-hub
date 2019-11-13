// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using WebApp.Config;

namespace WebApp.Models.Reporting
{
    public class IndexModel
    {
        public IndexModel(
            DateTimeOffset from,
            DateTimeOffset to,
            IEnumerable<RenderingEnvironment> environments,
            string nextMonth,
            string currentMonth,
            string prevMonth)
        {
            From = from;
            To = to;
            Environments = environments;
            NextMonthLink = nextMonth;
            CurrentMonthLink = currentMonth;
            PreviousMonthLink = prevMonth;
        }

        public DateTimeOffset From { get; }

        public DateTimeOffset To { get; }

        public Cost SummaryUsage { get; }

        public IEnumerable<RenderingEnvironment> Environments { get; }

        public string NextMonthLink { get; }

        public string CurrentMonthLink { get; }

        public string PreviousMonthLink { get; }

    }

}
