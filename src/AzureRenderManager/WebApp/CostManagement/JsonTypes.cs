// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WebApp.CostManagement
{
    // Cost Management do not yet have an SDK so this is in lieu of that.
    // Docs: https://docs.microsoft.com/en-us/rest/api/cost-management/query/usagebyresourcegroup

    public class ErrorDetails
    {
        public ErrorDetails(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; }
        public string Message { get; }

        public override string ToString()
            => $"{Code}: {Message}";
    }

    public class UsageResponse
    {
        public UsageResponse(UsageResponseProperties properties, ErrorDetails error)
        {
            Properties = properties;
            Error = error;
        }

        public UsageResponseProperties Properties { get; }

        public ErrorDetails Error { get; }
    }

    public class UsageResponseProperties
    {
        public UsageResponseProperties(List<Column> columns, List<List<object>> rows)
        {
            Columns = columns;
            Rows = rows;
        }

        public List<Column> Columns { get; }

        public List<List<object>> Rows { get; }
    }

    public class Column
    {
        public Column(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        public string Type { get; }
    }


    public class UsageRequest
    {
        public UsageRequest(Timeframe timeframe, Dataset dataset)
        {
            Timeframe = timeframe;
            Dataset = dataset;
        }

        public string Type { get; } = "Usage";

        [JsonConverter(typeof(StringEnumConverter))]
        public Timeframe Timeframe { get; }

        public Dataset Dataset { get; }
    }

    public class Dataset
    {
        public Dataset(
            Granularity granularity,
            Dictionary<string, Aggregation> aggregation,
            List<Grouping> grouping,
            FilterExpression filter)
        {
            Granularity = granularity;
            Aggregation = aggregation;
            Grouping = grouping;
            Filter = filter;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public Granularity Granularity { get; }

        public Dictionary<string, Aggregation> Aggregation { get; }

        public List<Grouping> Grouping { get; }

        public FilterExpression Filter { get; }
    }

    public class Grouping
    {
        public Grouping(string name, ColumnType type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ColumnType Type { get; }
    }

    public enum ColumnType
    {
        Dimension, Tag
    }

    public class Aggregation
    {
        public Aggregation(AggregationFunction function, string name)
        {
            Function = function;
            Name = name;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public AggregationFunction Function { get; }

        public string Name { get; }
    }

    public enum AggregationFunction
    {
        Sum
    }

    public abstract class FilterExpression
    {
        public static FilterExpression Or(params FilterExpression[] expressions)
            => new OrExpression(expressions);

        public static FilterExpression And(params FilterExpression[] expressions)
            => new AndExpression(expressions);

        public static FilterExpression Dimension(string name, Operator op, IEnumerable<string> values)
            => new DimensionExpression(new OperatorExpression(name, op, values));

        public static FilterExpression Tag(string name, Operator op, IEnumerable<string> values)
            => new TagExpression(new OperatorExpression(name, op, values));

        private class AndExpression : FilterExpression
        {
            public AndExpression(params FilterExpression[] and)
            {
                And = and.ToArray();
            }

            public new IReadOnlyList<FilterExpression> And { get; }
        }

        private class OrExpression : FilterExpression
        {
            public OrExpression(params FilterExpression[] or)
            {
                Or = or.ToArray();
            }

            public new IReadOnlyList<FilterExpression> Or { get; }
        }

        private class TagExpression : FilterExpression
        {
            public TagExpression(OperatorExpression expression)
                => Tags = expression;

            public OperatorExpression Tags { get; }
        }

        private class DimensionExpression : FilterExpression
        {
            public DimensionExpression(OperatorExpression expression)
                => Dimension = expression;

            public new OperatorExpression Dimension { get; }
        }

        private class OperatorExpression
        {
            public OperatorExpression(string name, Operator op, IEnumerable<string> values)
            {
                Name = name;
                Operator = op;
                Values = values.ToList();
            }

            public string Name { get; }

            [JsonConverter(typeof(StringEnumConverter))]
            public Operator Operator { get; }

            public IReadOnlyList<string> Values { get; }
        }
    }

    public enum Timeframe
    {
        MonthToDate,
        YearToDate,
        WeekToDate,
    }

    public enum Granularity
    {
        Daily
    }

    public enum Operator
    {
        Contains,
        In
    }
}
