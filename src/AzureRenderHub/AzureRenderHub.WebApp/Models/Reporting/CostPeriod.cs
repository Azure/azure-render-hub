using Microsoft.AspNetCore.Mvc;
using System;
using WebApp.CostManagement;

namespace AzureRenderHub.WebApp.Models.Reporting
{
    public class CostPeriod
    {
        private readonly IUrlHelper _urlHelper;
        private readonly string _routeName;

        public CostPeriod(string routeName, IUrlHelper urlHelper, DateTimeOffset? from, DateTimeOffset? to)
        {
            _routeName = routeName;
            _urlHelper = urlHelper;
            QueryTimePeriod = GetQueryPeriod(from, to);
        }

        public QueryTimePeriod QueryTimePeriod { get; }

        public string GetPrevMonthLink()
            => _urlHelper.RouteUrl(_routeName, new { from = PrevMonthStart(QueryTimePeriod.From), to = PrevMonthEnd(QueryTimePeriod.From) });

        public string GetNextMonthLink()
        {
            var now = DateTimeOffset.UtcNow;
            if (QueryTimePeriod.To >= now)
            {
                return null;
            }

            return _urlHelper.RouteUrl(_routeName, new { from = NextMonthStart(QueryTimePeriod.To), to = NextMonthEnd(QueryTimePeriod.To) });
        }

        public string GetCurrentMonthLink()
        {
            var thisMonth = ThisMonth();
            return _urlHelper.RouteUrl(_routeName, new { from = thisMonth.From, to = thisMonth.To });
        }

        private static QueryTimePeriod GetQueryPeriod(DateTimeOffset? from, DateTimeOffset? to)
        {
            if (from == null && to == null)
            {
                return ThisMonth();
            }
            else if (to == null)
            {
                return new QueryTimePeriod(from: from.Value, to: EndOfMonth(from.Value));
            }
            else if (from == null)
            {
                return new QueryTimePeriod(from: StartOfMonth(to.Value), to: to.Value);
            }
            else
            {
                return new QueryTimePeriod(from: from.Value, to: to.Value);
            }
        }

        private static DateTimeOffset NextMonthStart(DateTimeOffset value)
            => StartOfMonth(value).AddMonths(1);

        private static DateTimeOffset NextMonthEnd(DateTimeOffset value)
            => EndOfMonth(NextMonthStart(value));

        private static DateTimeOffset PrevMonthStart(DateTimeOffset value)
            => StartOfMonth(value).AddMonths(-1);

        private static DateTimeOffset PrevMonthEnd(DateTimeOffset value)
            => EndOfMonth(PrevMonthStart(value));

        public static DateTimeOffset StartOfMonth(DateTimeOffset value)
            => new DateTimeOffset(value.Year, value.Month, 1, 0, 0, 0, value.Offset);

        public static DateTimeOffset EndOfMonth(DateTimeOffset value)
            => NextMonthStart(value) - TimeSpan.FromSeconds(1);

        private static QueryTimePeriod ThisMonth()
        {
            var today = DateTimeOffset.UtcNow;
            return new QueryTimePeriod(from: StartOfMonth(today), to: EndOfMonth(today));
        }
    }
}
