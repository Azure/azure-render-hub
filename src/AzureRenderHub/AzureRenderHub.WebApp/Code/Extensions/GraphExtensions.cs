using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Code.Extensions
{
    public static class GraphExtensions
    {
        public static string FindEmailAddress(this User user)
        {
            if (!string.IsNullOrEmpty(user.Mail))
            {
                return user.Mail;
            }
            return user.UserPrincipalName;
        }
    }
}
