using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApp.Code.Extensions
{
    public static class ClaimsExtensions
    {
        private const string NameClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
        private const string ObjectIdClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string TenantIdClaim = "http://schemas.microsoft.com/identity/claims/tenantid";
        private const string EmailAddressClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
        private const string EmailAddressAdFsClaim = "http://schemas.xmlsoap.org/claims/EmailAddress";
        private const string UpnClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";
        private const string UpnAdFsClaim = "http://schemas.xmlsoap.org/claims/UPN";
        private const string IssuerValue = "iss";

        public static string GetName(this IEnumerable<Claim> claims)
        {
            var name = claims.FirstOrDefault(c => c.Type == NameClaim)?.Value;
            // names from Live and other accounts are prefixed with the provider,
            // i.e. live.com#bob@contoso.com
            if (name != null && name.Contains("#"))
            {
                name = name.Split("#")[1];
            }
            return name;
        }

        public static string GetEmailAddress(this IEnumerable<Claim> claims)
        {
            var email = claims.FirstOrDefault(c => c.Type == EmailAddressClaim)?.Value;
            if (email == null)
            {
                email = claims.FirstOrDefault(c => c.Type == EmailAddressAdFsClaim)?.Value;
            }
            return email;
        }

        public static string GetUpn(this IEnumerable<Claim> claims)
        {
            var upn = claims.FirstOrDefault(c => c.Type == UpnClaim)?.Value;
            if (upn == null)
            {
                upn = claims.FirstOrDefault(c => c.Type == UpnAdFsClaim)?.Value;
            }
            return upn;
        }

        public static string GetObjectId(this IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c => c.Type == ObjectIdClaim)?.Value;
        }

        public static string GetTenantId(this IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c => c.Type == TenantIdClaim)?.Value;
        }

        public static string FindFirstValueOrThrow(this ClaimsPrincipal principal, string claimType)
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, 
                    "The supplied principal does not contain a claim of type {0}", claimType));
            }
            return value;
        }

        public static string GetIssuerValue(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(IssuerValue);
        }
    }
}
