using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Config;

namespace WebApp.Models.Environments
{
    public class EnvironmentUserPermissionsModel : EnvironmentBaseModel
    {
        // For view Submission/POST
        public EnvironmentUserPermissionsModel() { }

        public EnvironmentUserPermissionsModel(RenderingEnvironment environment)
        {
            EnvironmentName = environment.Name;
            RenderManager = environment.RenderManager;
        }

        public List<UserPermission> ClassicAdministrators { get; set; } = new List<UserPermission>();

        public List<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

        public string Error { get; set; }

        public string ErrorMessage { get; set; }

        [EmailAddress]
        public string EmailAddress { get; set; }
        
        [Required]
        [EnumDataType(typeof(PortalRole))]
        public PortalRole UserRole { get; set; }
    }

    public enum PortalRole {
        Reader, // Reader
        PoolManager, // Reader and Create Pools
        Owner, // Assign
    }
}
