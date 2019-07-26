using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureRenderHub.WebApp.Providers.Logs
{
    public class LogProvider : ILogProvider
    {
        public IEnumerable<string> GetAllLogs(DateTimeOffset startTime, DateTimeOffset? endTime = null)
        {
            return new List<string>
            {
                "This is my system log",
            };
        }

        public IEnumerable<string> GetAutoScaleLogs(DateTimeOffset startTime, DateTimeOffset? endTime = null)
        {
            return new List<string>
            {
                "Auto scale log",
            };
        }
    }
}
