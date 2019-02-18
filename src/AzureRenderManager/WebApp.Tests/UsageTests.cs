using System;
using System.Collections.Generic;
using System.Text;
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
            var usage1 = new Usage(period, new UsageResponse(new UsageResponseProperties(new List<Column>(), new List<List<object>>()), null));
            var usage2 = new Usage(period, new UsageResponse(new UsageResponseProperties(new List<Column>(), new List<List<object>>()), null));

            Assert.NotNull(new Usage(usage1, usage2));
            Assert.NotNull(new Usage(usage2, usage1));
        }
    }
}
