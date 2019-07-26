using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureRenderHub.WebApp.Providers.Logs
{
    public interface ILogProvider
    {
        IEnumerable<string> GetAllLogs(DateTimeOffset startTime, DateTimeOffset? endTime = null);

        IEnumerable<string> GetAutoScaleLogs(DateTimeOffset startTime, DateTimeOffset? endTime = null);
    }
}
