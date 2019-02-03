using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using WebApp.Arm;
using WebApp.Config;
using Xunit;

namespace WebApp.Tests
{
#if TODO
    public class SecureConfigurationStoreTests
    {
        [Fact]
        public async Task VerifyCredentialsSet()
        {
            var kvClient = new FakeKeyVaultClient();
            var secureConfigStore = new SecureConfigurationStore(kvClient);
            var portalConfig = GetPortalConfig();
            await secureConfigStore.SetConfig(portalConfig);

            Assert.Equal("theapikey", await kvClient.GetKeyVaultSecretAsync(Guid.Empty, null, "ApplicationInsightsApiKey"));
            Assert.Equal("domainpassword", await kvClient.GetKeyVaultSecretAsync(Guid.Empty, null, "DomainJoinPassword"));
            Assert.Equal("sppassword", await kvClient.GetKeyVaultSecretAsync(Guid.Empty, null, "ServicePrincipalPassword"));
        }

        private PortalConfiguration GetPortalConfig()
        {
            return new PortalConfiguration
            {
                Environments = new List<RenderingEnvironment>(
                    new[] { new RenderingEnvironment
                    {
                        ApplicationInsightsAccount = new ApplicationInsightsAccount
                        {
                            ApplicationId = "appId",
                            ApiKey = "theapikey",
                        },
                        Domain = new DomainConfig
                        {
                            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Test, not a real cred")]
                            DomainJoinPassword = "domainpassword"
                        },
                        ManagementServicePrincipal = new ServicePrincipal
                        {
                            Name = "SP",
                            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Test, not a real cred")]
                            Password = "sppassword"
                        },
                        KeyVault = new KeyVault(),
                    }})
            };
        }
    }

    class FakeKeyVaultClient : IKeyVaultMsiClient
    {
        private readonly Dictionary<string, string> _secrets = new Dictionary<string, string>();

        public async Task<X509Certificate2> GetKeyVaultCertificateAsync(Guid subscriptionId, KeyVault keyVault, string certificateName, string password)
        {
            await Task.Delay(0);
            throw new NotImplementedException();
        }

        public async Task<string> GetKeyVaultSecretAsync(Guid subscriptionId, KeyVault keyVault, string secretName)
        {
            await Task.Delay(0);
            return _secrets.TryGetValue(secretName, out var v) ? v : null;
        }

        public async Task SetKeyVaultSecretAsync(Guid subscriptionId, KeyVault keyVault, string secretName, string value)
        {
            await Task.Delay(0);
            _secrets[secretName] = value;
        }
    }
#endif
}
