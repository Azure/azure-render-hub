using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models.Environments
{
    public class UserPermission
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string ObjectId { get; set; }

        public string Role { get; set; }

        public string Scope { get; set; }

        public List<string> Actions { get; set; }

        // True if we were unable to find the User in Graph,
        // i.e. no permissions to query the API.
        public bool GraphResolutionFailure { get; set; }
    }
}
