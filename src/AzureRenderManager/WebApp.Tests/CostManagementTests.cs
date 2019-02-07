// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using WebApp.CostManagement;
using Xunit;

namespace WebApp.Tests
{
    // Some simple tests to ensure we're generating the JSON correctly.
    // Note that the examples on the REST API are not correct for this
    // version of the API, so they have been tweaked.
    public class CostManagementTests
    {
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

            Assert.Equal(
                example,
                JsonConvert.SerializeObject(
                    request,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    }));
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
    }
}
