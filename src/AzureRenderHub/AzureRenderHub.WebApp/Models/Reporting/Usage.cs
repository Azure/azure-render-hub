// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WebApp.CostManagement;

namespace WebApp.Models.Reporting
{
    /// <summary>
    /// A more usable presentation of the data from <see cref="UsageResponse"/>
    /// </summary>
    public sealed class Cost
    {
        public Cost(
            QueryTimePeriod period,
            string currency,
            IReadOnlyDictionary<string, SortedDictionary<DateTimeOffset, double>> categorized,
            double total)
        {
            Period = period;
            Currency = currency;
            Categorized = categorized;
            Total = total;
        }

        /// <summary>
        /// Merges two usages into one. They must be for the same time period.
        /// </summary>
        public static Cost Aggregate(Cost left, Cost right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (left.Period != right.Period)
            {
                throw new ArgumentException("Can't combine usages from two different periods");
            }

            if (left.Currency != right.Currency)
            {
                if (left.Currency != null && right.Currency != null)
                {
                    throw new ArgumentException("Can't combine usages with two different currencies");
                }

                // if one is null then there's no data in that side, and it's okay to merge
            }

            return new Cost(
                left.Period,
                left.Currency ?? right.Currency,
                MergeData(left.Categorized, right.Categorized),
                left.Total + right.Total);
        }

        /// <summary>
        /// Merges all cost categories into one new category.
        /// </summary>
        public Cost Recategorize(string categoryName)
        {
            var squishedData = new SortedDictionary<DateTimeOffset, double>();
            foreach (var category in Categorized.Values)
            {
                foreach (var kvp in category)
                {
                    var existing = squishedData.TryGetValue(kvp.Key, out var currentTotal) ? currentTotal : 0;
                    squishedData[kvp.Key] = existing + kvp.Value;
                }
            }

            var categorized = new Dictionary<string, SortedDictionary<DateTimeOffset, double>> { { categoryName, squishedData } };
            return new Cost(Period, Currency, categorized, Total);
        }

        private static IReadOnlyDictionary<string, SortedDictionary<DateTimeOffset, double>> MergeData(
            IReadOnlyDictionary<string, SortedDictionary<DateTimeOffset, double>> left,
            IReadOnlyDictionary<string, SortedDictionary<DateTimeOffset, double>> right)
        {
            // clone left
            var result = new Dictionary<string, SortedDictionary<DateTimeOffset, double>>(left);

            // merge in right
            foreach (var kvp in right)
            {
                if (result.TryGetValue(kvp.Key, out var existing))
                {
                    result[kvp.Key] = new SortedDictionary<DateTimeOffset, double>(kvp.Value.Zip(existing, MergeValues).ToDictionary(it => it.Key, it => it.Value));
                }
                else
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;

            KeyValuePair<DateTimeOffset, double> MergeValues(KeyValuePair<DateTimeOffset, double> l, KeyValuePair<DateTimeOffset, double> r)
            {
                if (l.Key != r.Key)
                {
                    throw new ArgumentException("Merging data with mismatched dates");
                }

                return new KeyValuePair<DateTimeOffset, double>(l.Key, l.Value + r.Value);
            }
        }

        public Cost(QueryTimePeriod period, UsageResponse response)
        {
            Period = period;
            Currency = ExtractCurrency(response);
            Categorized = OrganizeData(period, response);
            Total = Categorized.Values.SelectMany(vs => vs.Values).Sum();
        }

        private static string ExtractCurrency(UsageResponse response)
        {
            var currencyIndex = response.Properties.Columns.FindIndex(col => col.Name == "Currency");
            return (string)response.Properties.Rows.FirstOrDefault()?[currencyIndex]; // assumption: all values have same currency(!)
        }

        private static IReadOnlyDictionary<string, SortedDictionary<DateTimeOffset, double>> OrganizeData(
            QueryTimePeriod period,
            UsageResponse response)
        {
            var result = new Dictionary<string, SortedDictionary<DateTimeOffset, double>>();

            var cols = response.Properties.Columns;
            var dateIndex = cols.FindIndex(col => col.Name == "UsageDate");
            var costIndex = cols.FindIndex(col => col.Name == "PreTaxCost");
            var serviceNameIndex = cols.FindIndex(col => col.Name == "ServiceName");

            foreach (var meterCategory in response.Properties.Rows.GroupBy(row => (string)row[serviceNameIndex]))
            {
                var category = string.IsNullOrWhiteSpace(meterCategory.Key) ? "uncategorized" : meterCategory.Key;
                result[category] = GenerateDataForCategory(category, meterCategory);
            }

            return result;

            SortedDictionary<DateTimeOffset, double> GenerateDataForCategory(string meterCategory, IEnumerable<IReadOnlyList<object>> days)
            {
                return new SortedDictionary<DateTimeOffset, double>(EnumerateDays().ToDictionary(kvp => kvp.Day, kvp => kvp.Cost));

                IEnumerable<(DateTimeOffset Day, double Cost)> EnumerateDays()
                {
                    var convertedDays = days.Select(x => new { date = ParseDate((long)x[dateIndex]), cost = (double)x[costIndex] });

                    var currentDay = period.From;
                    foreach (var day in convertedDays.OrderBy(d => d.date))
                    {
                        // we have to pad the data with zeroes because ChartJS can't handle it otherwise
                        currentDay = currentDay.AddDays(1);
                        while (currentDay < day.date)
                        {
                            yield return (currentDay, 0);
                            currentDay = currentDay.AddDays(1);
                        }

                        yield return (currentDay, day.cost);
                    }

                    // we have to pad the data with zeroes because ChartJS can't handle it otherwise
                    currentDay = currentDay.AddDays(1);
                    while (currentDay <= period.To)
                    {
                        yield return (currentDay, 0);
                        currentDay = currentDay.AddDays(1);
                    }
                }
            }
        }

        private static DateTimeOffset ParseDate(long input)
            => DateTimeOffset.ParseExact(
                input.ToString(CultureInfo.InvariantCulture),
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal);

        public string Currency { get; }

        public double Total { get; }

        public QueryTimePeriod Period { get; }

        // Inner dictionary should not be changed - want to express that it is already sorted.
        public IReadOnlyDictionary<string, SortedDictionary<DateTimeOffset, double>> Categorized { get; }
    }
}
