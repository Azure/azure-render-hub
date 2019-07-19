using System;
using System.Collections.Generic;
using System.Linq;
using WebApp.Config;
using WebApp.CostManagement;
using WebApp.Models.Reporting;
using Xunit;

namespace WebApp.Tests
{
    public class UsageTests
    {
        [Fact]
        public void CanMergeEmptyUsages()
        {
            var period = new QueryTimePeriod(DateTimeOffset.Now, DateTimeOffset.Now);
            var usage1 = new Cost(period, new UsageResponse(new UsageResponseProperties(new List<Column>(), new List<List<object>>()), null));
            var usage2 = new Cost(period, new UsageResponse(new UsageResponseProperties(new List<Column>(), new List<List<object>>()), null));

            Assert.NotNull(new Cost(usage1, usage2));
            Assert.NotNull(new Cost(usage2, usage1));
        }

        [Fact]
        public void AllResourceGroupNamesAreExtractedFromRenderingEnvironment()
        {
            // make sure if any more AzureResource objects are added to 
            // RenderingEnvironment, that ExtractResourceGroupNames is updated to match

            int countPopulated = 0;
            var env = PopulateEnv();

            var rgNames = env.ExtractResourceGroupNames();

            Assert.Equal(countPopulated, rgNames.Count());

            RenderingEnvironment PopulateEnv()
            {
                var result = new RenderingEnvironment();
                Populate(result);

                result.Name = NextName();
                return result;

                void Populate(object o)
                {
                    if (o == null)
                    {
                        return;
                    }

                    if (o is AzureResource r)
                    {
                        // create a fake resource ID - RG name is 5th component
                        r.ResourceId = "1/2/3/4/" + NextName();
                    }

                    foreach (var member in o.GetType().GetProperties().Where(p => p.CanWrite))
                    {
                        object inner;
                        try
                        {
                            inner = Activator.CreateInstance(member.PropertyType);
                        }
                        catch
                        {
                            continue;
                        }

                        Populate(inner);

                        member.SetValue(o, inner);
                    }
                }

                string NextName() => "rg_" + (++countPopulated);
            }
        }
    }
}
