// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace WebApp.Models.Reporting
{
    public class IndividualModel
    {
        public IndividualModel(
            DateTimeOffset from,
            DateTimeOffset to,
            EnvironmentCost usage,
            string nextMonth,
            string currentMonth,
            string prevMonth)
        {
            From = from;
            To = to;
            Usage = usage;
            NextMonthLink = nextMonth;
            CurrentMonthLink = currentMonth;
            PreviousMonthLink = prevMonth;
        }

        public DateTimeOffset From { get; }

        public DateTimeOffset To { get; }

        public EnvironmentCost Usage { get; }

        public string NextMonthLink { get; }

        public string CurrentMonthLink { get; }

        public string PreviousMonthLink { get; }
    }

}
