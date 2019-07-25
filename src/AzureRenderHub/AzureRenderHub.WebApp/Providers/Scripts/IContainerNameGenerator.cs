using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureRenderHub.WebApp.Providers.Scripts
{
    public interface IContainerNameGenerator
    {
        // Returns a safe (hashed) container name based on
        // a pool name.  Container naming rules are stricter than 
        // pool names
        string GetContainerName(string poolName);
    }
}
