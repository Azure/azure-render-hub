using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Config.Coordinators
{
    public interface IConfigRepository<T>
    {
        Task<T> Get(string configName);

        Task<bool> Remove(string configName);

        Task Update(T config, string configName, string originalName);

        Task<List<string>> List();
    }
}
