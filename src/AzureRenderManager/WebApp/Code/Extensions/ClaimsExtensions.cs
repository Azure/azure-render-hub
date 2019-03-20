using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApp.Code.Extensions
{
    public static class ClaimsExtensions
    {
        public static string GetName(this IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
        }

        public static string GetEmailAddress(this IEnumerable<Claim> claims)
        {
            var email = claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
            if (email == null)
            {
                email = claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/claims/EmailAddress")?.Value;
            }
            return email;
        }

        public static string GetUpn(this IEnumerable<Claim> claims)
        {
            var upn = claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value;
            if (upn == null)
            {
                upn = claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/claims/UPN")?.Value;
            }
            return upn;
        }

        public static string GetObjectId(this IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        }

        public static string GetTenantId(this IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
        }
    }
}
