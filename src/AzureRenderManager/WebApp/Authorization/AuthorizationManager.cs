using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Arm;
using WebApp.Config;
using WebApp.Models.Environments;
using TaskTupleAwaiter;
using WebApp.Code.Graph;
using Microsoft.Graph;
using System.Net.Http.Headers;
using WebApp.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace WebApp.Authorization
{
    public class AuthorizationManager: NeedsAccessToken
    {
        private readonly IAzureResourceProvider _azureResourceProvider;
        private readonly IGraphAuthProvider _graphProvider;
        private readonly IConfiguration _configuration;

        public AuthorizationManager(
            IHttpContextAccessor contextAccessor,
            IConfiguration configuration,
            IAzureResourceProvider azureResourceProvider,
            IGraphAuthProvider graphProvider) : base(contextAccessor)
        {
            _azureResourceProvider = azureResourceProvider;
            _configuration = configuration;
            _graphProvider = graphProvider;
        }

        public async Task<List<UserPermission>> ListClassicAdministrators(RenderingEnvironment environment)
        {
            var classicAdmins = await _azureResourceProvider.ListClassicAdministrators(environment.SubscriptionId);

            var permissions = new List<UserPermission>();

            foreach (var admin in classicAdmins)
            {
                permissions.Add(new UserPermission
                {
                    Name = admin.EmailAddress, // The 'Name' property doesn't actually contain a name
                    Email = admin.EmailAddress,
                    Role = admin.Role,
                });
            }

            return permissions;
        }

        public async Task<List<UserPermission>> ListUserPermissions(RenderingEnvironment environment)
        {
            var (resourceGroupPermissions, 
                batchPermissions, 
                storagePermissions, 
                keyVaultPermissions, 
                appInsightsPermissions,
                vnetPermissions) = await (
                _azureResourceProvider.GetUserPermissions(environment.SubscriptionId, environment.ResourceGroupResourceId),
                _azureResourceProvider.GetUserPermissions(environment.SubscriptionId, environment.BatchAccount.ResourceId),
                _azureResourceProvider.GetUserPermissions(environment.SubscriptionId, environment.StorageAccount.ResourceId),
                _azureResourceProvider.GetUserPermissions(environment.SubscriptionId, environment.KeyVault.ResourceId),
                _azureResourceProvider.GetUserPermissions(environment.SubscriptionId, environment.ApplicationInsightsAccount.ResourceId),
                _azureResourceProvider.GetUserPermissions(environment.SubscriptionId, environment.Subnet.VnetResourceId));

            var permissions = new List<UserPermission>();
            permissions.AddRange(resourceGroupPermissions);
            permissions.AddRange(batchPermissions);
            permissions.AddRange(storagePermissions);
            permissions.AddRange(keyVaultPermissions);
            permissions.AddRange(appInsightsPermissions);
            permissions.AddRange(vnetPermissions);

            var ownerRoles = new[] { "Owner" };
            var poolManagerRoles = new[] { "Owner", "Contributor", "Reader" };
            var readerRoles = new[] { "Owner", "Contributor", "Reader" };

            var finalPermissions = new List<UserPermission>();
            var uniqueUsers = permissions.Select(p => p.ObjectId).ToHashSet();
            foreach (var objectId in uniqueUsers)
            {

                var userResourceGroupPermissions = resourceGroupPermissions.Where(p => p.ObjectId == objectId).ToList();
                var userBatchPermissions = batchPermissions.Where(p => p.ObjectId == objectId).ToList();
                var userStoragePermissions = storagePermissions.Where(p => p.ObjectId == objectId).ToList();
                var userKeyVaultPermissions = keyVaultPermissions.Where(p => p.ObjectId == objectId).ToList();
                var userAppInsightsPermissions = appInsightsPermissions.Where(p => p.ObjectId == objectId).ToList();
                var userVnetPermissions = vnetPermissions.Where(p => p.ObjectId == objectId).ToList();

                // Owner
                if (userResourceGroupPermissions.Any(p => p.Role == "Owner") &&
                    userBatchPermissions.Any(p => p.Role == "Owner") &&
                    userStoragePermissions.Any(p => p.Role == "Owner") &&
                    userKeyVaultPermissions.Any(p => p.Role == "Owner") &&
                    userAppInsightsPermissions.Any(p => p.Role == "Owner") &&
                    userVnetPermissions.Any(p => p.Role == "Owner"))
                {
                    finalPermissions.Add(new UserPermission
                    {
                        ObjectId = objectId,
                        Email = permissions.First(p => p.ObjectId == objectId).Email,
                        Name = permissions.First(p => p.ObjectId == objectId).Name,
                        Role = PortalRole.Owner.ToString(),
                    });
                }
                // Pool Manager
                else if (userResourceGroupPermissions.Any(p => poolManagerRoles.Contains(p.Role)) &&
                    userBatchPermissions.Any(p => p.Role == "Owner" || p.Role == "Contributor") &&
                    userStoragePermissions.Any(p => poolManagerRoles.Contains(p.Role)) &&
                    userKeyVaultPermissions.Any(p => poolManagerRoles.Contains(p.Role)) &&
                    userAppInsightsPermissions.Any(p => poolManagerRoles.Contains(p.Role)) &&
                    userVnetPermissions.Any(p => poolManagerRoles.Contains(p.Role) || p.Role == "Virtual Machine Contributor"))
                {
                    finalPermissions.Add(new UserPermission
                    {
                        ObjectId = objectId,
                        Email = permissions.First(p => p.ObjectId == objectId).Email,
                        Name = permissions.First(p => p.ObjectId == objectId).Name,
                        Role = PortalRole.PoolManager.ToString(),
                    });
                }
                // Reader
                else if (userResourceGroupPermissions.Any(p => readerRoles.Contains(p.Role)) &&
                    userBatchPermissions.Any(p => readerRoles.Contains(p.Role)) &&
                    userStoragePermissions.Any(p => readerRoles.Contains(p.Role)) &&
                    userKeyVaultPermissions.Any(p => readerRoles.Contains(p.Role)) &&
                    userAppInsightsPermissions.Any(p => readerRoles.Contains(p.Role)) &&
                    userVnetPermissions.Any(p => readerRoles.Contains(p.Role)))
                {

                    finalPermissions.Add(new UserPermission
                    {
                        ObjectId = objectId,
                        Email = permissions.First(p => p.ObjectId == objectId).Email,
                        Name = permissions.First(p => p.ObjectId == objectId).Name,
                        Role = PortalRole.Reader.ToString(),
                    });
                }
            }

            return finalPermissions;
        }

        private static EnvironmentRoleAssignments ReaderRoles = new EnvironmentRoleAssignments
        {
            EnvironmentResourceGroupRole = "Reader",
            BatchRole = "Reader",
            StorageRole = "Reader",
            KeyVaultRole = "Reader",
            ApplicationInsightsRole = "Reader",
            VNetRole = "Reader",
        };

        private static EnvironmentRoleAssignments PoolManagerRoles = new EnvironmentRoleAssignments
        {
            EnvironmentResourceGroupRole = "Reader",
            BatchRole = "Contributor",
            StorageRole = "Reader",
            KeyVaultRole = "Reader",
            ApplicationInsightsRole = "Reader",
            VNetRole = "Virtual Machine Contributor",
        };

        private static EnvironmentRoleAssignments OwnerRoles = new EnvironmentRoleAssignments
        {
            EnvironmentResourceGroupRole = "Owner",
            BatchRole = "Owner",
            StorageRole = "Owner",
            KeyVaultRole = "Owner",
            ApplicationInsightsRole = "Owner",
            VNetRole = "Owner",
        };

        public async Task AssignRoleToUser(RenderingEnvironment environment, string userEmailAddress, PortalRole userRole)
        {
            var graphServiceClient = new GraphServiceClient(new DelegateAuthenticationProvider(async requestMessage =>
            {
               var graphAccessToken = await _graphProvider.GetUserAccessTokenAsync(GetUser());
               requestMessage
                   .Headers
                   .Authorization = new AuthenticationHeaderValue("Bearer", graphAccessToken);
            }));

            List<QueryOption> options = new List<QueryOption>
            {
                    new QueryOption("$search", "lunch")
            };

            var signInNamesFilter = $"signInNames/any(c:c/value eq '{userEmailAddress}')";
            var otherMailsFilter = $"otherMails/any(x:x/value eq '{userEmailAddress}')";
            var externalEmail = $"{userEmailAddress.Replace('@', '_')}#EXT#@{_configuration["AzureAd:Domain"]}";
            var user = await graphServiceClient.Users.Request().Filter(
                $"mail eq '{userEmailAddress}' or " +
                $"userPrincipalName eq '{userEmailAddress}' or " +
                $"userPrincipalName eq '{externalEmail}'").GetAsync();

            if (user == null || user.Count == 0)
            {
                throw new Exception($"User {userEmailAddress} not found");
            }

            if (user.Count > 1)
            {
                throw new Exception($"Multiple users found for email {userEmailAddress}");
            }

            var objectId = user.First().Id;

            EnvironmentRoleAssignments roleAssignments = null;
            switch (userRole)
            {
                case PortalRole.Reader:
                    roleAssignments = ReaderRoles;
                    break;
                case PortalRole.PoolManager:
                    roleAssignments = PoolManagerRoles;
                    break;
                case PortalRole.Owner:
                    roleAssignments = OwnerRoles;
                    break;
            }

            if (roleAssignments == null)
            {
                throw new Exception($"No role assignments configured for role {userRole}");
            }

            await AssignRolesToUser(objectId, environment, roleAssignments);
        }

        private async Task AssignRolesToUser(string objectId, RenderingEnvironment environment, EnvironmentRoleAssignments roleAssignments)
        {
            var identity = new Identity.Identity { ObjectId = Guid.Parse(objectId) };

            await (_azureResourceProvider.AssignManagementIdentityAsync(
                environment.SubscriptionId,
                environment.ResourceGroupResourceId,
                roleAssignments.EnvironmentResourceGroupRole,
                identity),

                _azureResourceProvider.AssignManagementIdentityAsync(
                environment.SubscriptionId,
                environment.BatchAccount.ResourceId,
                roleAssignments.BatchRole,
                identity),

                _azureResourceProvider.AssignManagementIdentityAsync(
                environment.SubscriptionId,
                environment.StorageAccount.ResourceId,
                roleAssignments.StorageRole,
                identity),

                _azureResourceProvider.AssignManagementIdentityAsync(
                environment.SubscriptionId,
                environment.KeyVault.ResourceId,
                roleAssignments.KeyVaultRole,
                identity),

                _azureResourceProvider.AssignManagementIdentityAsync(
                environment.SubscriptionId,
                environment.ApplicationInsightsAccount.ResourceId,
                roleAssignments.ApplicationInsightsRole,
                identity),

                _azureResourceProvider.AssignManagementIdentityAsync(
                environment.SubscriptionId,
                environment.Subnet.VnetResourceId,
                roleAssignments.VNetRole,
                identity)
                );
        }

        private List<UserPermission> MapUserPermissionsToPortalRoles(List<UserPermission> userPermissions)
        {
            var mappedPermissions = new List<UserPermission>();
            var userToPermissionsDict = userPermissions
                             .GroupBy(x => x.ObjectId)
                             .ToDictionary(gdc => gdc.Key, gdc => gdc.ToHashSet());
            //foreach (var userPermission in userToPermissionsDict)
           // {

            //}
            return mappedPermissions;
        }

        class EnvironmentRoleAssignments
        {
            public string EnvironmentResourceGroupRole { get; set; }
            public string BatchRole { get; set; }
            public string StorageRole { get; set; }
            public string KeyVaultRole { get; set; }
            public string ApplicationInsightsRole { get; set; }
            public string VNetRole { get; set; }
        }
    }
}
