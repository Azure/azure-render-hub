using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApp.Authorization
{
    public interface IGraphProvider
    {
        Task<Dictionary<string, User>> LookupObjectIdsAsync(ClaimsPrincipal claim, IList<string> userIds);

        Task<User> GetUser(ClaimsPrincipal claim, string userEmailAddress);
    }
}
