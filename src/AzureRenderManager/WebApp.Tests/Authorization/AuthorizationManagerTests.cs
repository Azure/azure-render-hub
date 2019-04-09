using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApp.Authorization;
using WebApp.Config;
using WebApp.Models.Environments;
using WebApp.Tests.Fakes;
using Xunit;

namespace WebApp.Tests.Authorization
{
    public class AuthorizationManagerTests
    {
        [Fact]
        public async Task SubscriptionOwnerIsEnvOwner()
        {
            var arm = new FakeAzureResourceProvider();
            var graphProvider = new AuthorizationManager(null, null, arm, null);
            var env = RenderingEnvironment;

            arm.UserPermissions.Add(new UserPermission {
                ObjectId = Guid.NewGuid().ToString(),
                Scope = $"/subscriptions/{env.SubscriptionId}",
                Role = "Owner"
            });

            var perms = await graphProvider.ListUserPermissions(env);

            var perm = Assert.Single(perms);
            Assert.Equal(PortalRole.Owner.ToString(), perm.Role);
        }

        [Fact]
        public async Task SubscriptionReaderIsEnvReader()
        {
            var arm = new FakeAzureResourceProvider();
            var graphProvider = new AuthorizationManager(null, null, arm, null);
            var env = RenderingEnvironment;

            arm.UserPermissions.Add(new UserPermission
            {
                ObjectId = Guid.NewGuid().ToString(),
                Scope = $"/subscriptions/{env.SubscriptionId}",
                Role = "Reader"
            });

            var perms = await graphProvider.ListUserPermissions(env);

            var perm = Assert.Single(perms);
            Assert.Equal(PortalRole.Reader.ToString(), perm.Role);
        }

        [Fact]
        public async Task SubscriptionContributorIsEnvPoolManager()
        {
            var arm = new FakeAzureResourceProvider();
            var graphProvider = new AuthorizationManager(null, null, arm, null);
            var env = RenderingEnvironment;

            arm.UserPermissions.Add(new UserPermission
            {
                ObjectId = Guid.NewGuid().ToString(),
                Scope = $"/subscriptions/{env.SubscriptionId}",
                Role = "Contributor"
            });

            var perms = await graphProvider.ListUserPermissions(env);

            var perm = Assert.Single(perms);
            Assert.Equal(PortalRole.PoolManager.ToString(), perm.Role);
        }

        [Fact]
        public async Task IndividualResourceOwnerIsEnvOwner()
        {
            var arm = new FakeAzureResourceProvider();
            var graphProvider = new AuthorizationManager(null, null, arm, null);
            var env = RenderingEnvironment;

            var objectId = Guid.NewGuid().ToString();
            arm.UserPermissions.Add(new UserPermission { ObjectId = objectId, Scope = env.ResourceGroupResourceId, Role = "Owner" });
            arm.UserPermissions.Add(new UserPermission { ObjectId = objectId, Scope = env.BatchAccount.ResourceId, Role = "Owner" });
            arm.UserPermissions.Add(new UserPermission { ObjectId = objectId, Scope = env.StorageAccount.ResourceId, Role = "Owner" });
            arm.UserPermissions.Add(new UserPermission { ObjectId = objectId, Scope = env.KeyVault.ResourceId, Role = "Owner" });
            arm.UserPermissions.Add(new UserPermission { ObjectId = objectId, Scope = env.ApplicationInsightsAccount.ResourceId, Role = "Owner" });
            arm.UserPermissions.Add(new UserPermission { ObjectId = objectId, Scope = env.Subnet.VnetResourceId, Role = "Owner" });

            var perms = await graphProvider.ListUserPermissions(env);

            var perm = Assert.Single(perms);
            Assert.Equal(PortalRole.Owner.ToString(), perm.Role);
        }

        [Fact]
        public async Task MissingResourcePermissionMeansNoEnvPermissions()
        {
            var arm = new FakeAzureResourceProvider();
            var graphProvider = new AuthorizationManager(null, null, arm, null);
            var env = RenderingEnvironment;

            var objectId = Guid.NewGuid().ToString();

            // Test with each of the resource permissions missing
            for (var i = 0; i < 5; i++)
            {
                arm.UserPermissions.Clear();
                arm.UserPermissions.Add(new UserPermission { ObjectId = objectId, Scope = env.BatchAccount.ResourceId, Role = "Owner" });
                arm.UserPermissions.Add(new UserPermission { ObjectId = objectId, Scope = env.StorageAccount.ResourceId, Role = "Owner" });
                arm.UserPermissions.Add(new UserPermission { ObjectId = objectId, Scope = env.KeyVault.ResourceId, Role = "Owner" });
                arm.UserPermissions.Add(new UserPermission { ObjectId = objectId, Scope = env.ApplicationInsightsAccount.ResourceId, Role = "Owner" });
                arm.UserPermissions.Add(new UserPermission { ObjectId = objectId, Scope = env.Subnet.VnetResourceId, Role = "Owner" });

                // Remove a resource permission
                arm.UserPermissions.RemoveAt(i);

                var perms = await graphProvider.ListUserPermissions(env);
                Assert.Empty(perms);
            }
        }

        private RenderingEnvironment RenderingEnvironment { get
            {
                var sub = Guid.NewGuid();
                return new RenderingEnvironment
                {
                    SubscriptionId = sub,
                    Name = "fake",
                    BatchAccount = new BatchAccount { ResourceId = $"/subscriptions/{sub}/resourceGroups/fake-rg/Microsoft.Batch/batchAccounts/fakebatchaccount" },
                    StorageAccount = new StorageAccount { ResourceId = $"/subscriptions/{sub}/resourceGroups/fake-rg/Microsoft.Storage/storageAccounts/fakestorage" },
                    ApplicationInsightsAccount = new ApplicationInsightsAccount { ResourceId = $"/subscriptions/{sub}/resourceGroups/fake-rg/Microsoft.Insights/components/fakeappinsights" },
                    KeyVault = new KeyVault { ResourceId = $"/subscriptions/{sub}/resourceGroups/fake-rg/Microsoft.KeyVault/vaults/fakestorage" },
                    Subnet = new Subnet { ResourceId = $"/subscriptions/{sub}/resourceGroups/fake-rg/Microsoft.Networking/virtualNetworks/fakevnet/subnets/fakesubnet" },
                };
            }
        }
    }
}
