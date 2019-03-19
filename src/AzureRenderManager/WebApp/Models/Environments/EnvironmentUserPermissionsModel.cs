using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Config;

namespace WebApp.Models.Environments
{
    public class EnvironmentUserPermissionsModel : EnvironmentBaseModel
    {
        public EnvironmentUserPermissionsModel(RenderingEnvironment environment)
        {
            EnvironmentName = environment.Name;
            RenderManager = environment.RenderManager;
        }

        public List<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }
}
