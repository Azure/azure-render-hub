using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Config.Coordinators
{
    public interface IGenericConfigCoordinator
    {
        Task<T> Get<T>(string configName);

        Task<bool> Remove(string configName);

        Task Update<T>(T config, string configName, string originalName = null);

        Task<List<string>> List();
    }
}
