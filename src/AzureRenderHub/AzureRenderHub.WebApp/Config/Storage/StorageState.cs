using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureRenderHub.WebApp.Config.Storage
{
    public enum StorageState
    {
        Creating,
        Failed,
        Ready,
        Deleting
    }
}
