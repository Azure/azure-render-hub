using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Config;

namespace WebApp.Code.Extensions
{
    public static class InstallationPackageTypeExtension
    {
        public static bool HasInstallationCommand(this InstallationPackageType type)
        {
            return type == InstallationPackageType.General || type == InstallationPackageType.Gpu;
        }
    }
}
