// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using AzureRenderHub.WebApp.Models.Reporting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using WebApp.Controllers;
using WebApp.CostManagement;
using WebApp.Models.Reporting;
using Xunit;

namespace WebApp.Tests
{
    // Some simple tests to ensure we're generating the JSON correctly.
    // Note that the examples on the REST API are not correct for this
    // version of the API, so they have been tweaked.
    public class CostManagementTests
    {
        private static readonly JsonSerializerSettings SerializerSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

        private static string Serialize(object obj)
            => JsonConvert.SerializeObject(obj, Formatting.Indented, SerializerSettings);

        [Fact]
        public void UsageRequestMatchesExample()
        {
            var example = @"{
  ""type"": ""Usage"",
  ""timeframe"": ""MonthToDate"",
  ""dataset"": {
    ""granularity"": ""Daily"",
    ""aggregation"": {},
    ""grouping"": [],
    ""filter"": {
      ""and"": [
        {
          ""or"": [
            {
              ""dimension"": {
                ""name"": ""ResourceLocation"",
                ""operator"": ""In"",
                ""values"": [
                  ""East US"",
                  ""West Europe""
                ]
              }
            },
            {
              ""tags"": {
                ""name"": ""Environment"",
                ""operator"": ""In"",
                ""values"": [
                  ""UAT"",
                  ""Prod""
                ]
              }
            }
          ]
        },
        {
          ""dimension"": {
            ""name"": ""ResourceGroup"",
            ""operator"": ""In"",
            ""values"": [
              ""API""
            ]
          }
        }
      ]
    }
  }
}";

            var request =
                new UsageRequest(
                    Timeframe.MonthToDate,
                    new Dataset(
                        Granularity.Daily,
                        new Dictionary<string, Aggregation>(),
                        new List<Grouping>(),
                        FilterExpression.And(
                            FilterExpression.Or(
                                FilterExpression.Dimension(
                                    "ResourceLocation",
                                    Operator.In,
                                    new[] { "East US", "West Europe" }),
                                FilterExpression.Tag(
                                    "Environment",
                                    Operator.In,
                                    new[] { "UAT", "Prod" })),
                            FilterExpression.Dimension(
                                "ResourceGroup",
                                Operator.In,
                                new[] {"API"}))));

            Assert.Equal(example, Serialize(request));
        }

        [Fact]
        public void CanParseUsageResponseExample()
        {
            var example = @"
    {
      ""id"": ""subscriptions/55312978-ba1b-415c-9304-c4b9c43c0481/resourcegroups/ScreenSharingTest-peer/providers/Microsoft.CostManagement/Query/9af9459d-441d-4055-9ed0-83d4c4a363fb"",
      ""name"": ""9af9459d-441d-4055-9ed0-83d4c4a363fb"",
      ""type"": ""microsoft.costmanagement/Query"",
      ""properties"": {
        ""nextLink"": null,
        ""columns"": [
          {
            ""name"": ""PreTaxCost"",
            ""type"": ""Number""
          },
          {
            ""name"": ""ResourceGroup"",
            ""type"": ""String""
          },
          {
            ""name"": ""UsageDate"",
            ""type"": ""Number""
          },
          {
            ""name"": ""Currency"",
            ""type"": ""String""
          }
        ],
        ""rows"": [
          [
            2.10333307059661,
            ""ScreenSharingTest-peer"",
            20180417,
            ""USD""
          ],
          [
            20.103333070596609,
            ""ScreenSharingTest-peer"",
            20180418,
            ""USD""
          ]
        ]
      }
    }";

            // weak test but ok
            var response = JsonConvert.DeserializeObject<UsageResponse>(example);
            Assert.NotNull(response.Properties);
        }

        [Fact]
        public void CanSerializeQueryTimePeriodAsExpected()
        {
            var timePeriod =
                new QueryTimePeriod(
                    from: new DateTimeOffset(2018, 11, 01, 00, 00, 00, TimeSpan.Zero),
                    to: new DateTimeOffset(2018, 11, 30, 23, 59, 59, TimeSpan.Zero));

            var expected = @"{
  ""from"": ""2018-11-01T00:00:00+00:00"",
  ""to"": ""2018-11-30T23:59:59+00:00""
}";

            Assert.Equal(expected, Serialize(timePeriod));
        }

        private static DateTimeOffset ParseDate(string input)
            => DateTimeOffset.Parse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

        [Theory]
        [InlineData("2018-01-01", "2018-01-01")] // Simple: from start
        [InlineData("2018-01-31", "2018-01-01")] // Simple: from last day
        [InlineData("2018-12-31", "2018-12-01")] // Year boundary end
        [InlineData("2020-02-29", "2020-02-01")] // Leap year
        [InlineData("2019-02-28", "2019-02-01")] // Not leap year
        public void StartOfMonthCalculationIsCorrect(string input, string expected)
        {
            var dto = ParseDate(input);
            var expectedDTO = ParseDate(expected);

            var actual = CostPeriod.StartOfMonth(dto);

            Assert.Equal(expectedDTO, actual);
        }

        [Theory]
        [InlineData("2018-01-01", "2018-01-31T23:59:59")] // Simple: from start
        [InlineData("2018-01-31", "2018-01-31T23:59:59")] // Simple: from last day
        [InlineData("2018-12-31", "2018-12-31T23:59:59")] // Year boundary
        [InlineData("2020-02-01", "2020-02-29T23:59:59")] // Leap year
        [InlineData("2019-02-01", "2019-02-28T23:59:59")] // Not leap year
        public void EndOfMonthCalculationIsCorrect(string input, string expected)
        {
            var dto = ParseDate(input);
            var expectedDTO = ParseDate(expected);

            var actual = CostPeriod.EndOfMonth(dto);

            Assert.Equal(expectedDTO, actual);
        }

        [Fact]
        public void CanRecategorizeCosts()
        {
            var start = DateTimeOffset.UtcNow;
            var mid = start + TimeSpan.FromMinutes(5);
            var end = start + TimeSpan.FromMinutes(10);

            var costs = new Cost(
                new QueryTimePeriod(start, end),
                "USD",
                new Dictionary<string, SortedDictionary<DateTimeOffset, double>>
                {
                    { "category1",
                      new SortedDictionary<DateTimeOffset, double>
                      {
                          { start, 1 },
                          { mid, 2 },
                      }
                    },
                    { "category2",
                      new SortedDictionary<DateTimeOffset, double>
                      {
                          { mid, 3 },
                          { end, 4 }
                      }
                    }
                },
                100);

            var recattedCosts = costs.Recategorize("newCat");

            var newCat = Assert.Single(recattedCosts.Categorized);
            Assert.Equal("newCat", newCat.Key);

            var values = newCat.Value.Select(kvp => (kvp.Key, kvp.Value)).ToList();
            Assert.Equal(
                new[] { (start, 1.0), (mid, 5), (end, 4) },
                values);
        }
    }
}
