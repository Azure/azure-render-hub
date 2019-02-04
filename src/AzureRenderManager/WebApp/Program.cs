// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using System.Threading.Tasks;

namespace WebApp
{
    public class Program
    {
        public static Task Main(string[] args)
            => CreateWebHost(args).RunAsync();

        public static IWebHost CreateWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var builtConfig = config.Build();
                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
                    var client = new KeyVaultClient(
                        new KeyVaultClient.AuthenticationCallback(
                            azureServiceTokenProvider.KeyVaultTokenCallback));
                    config.AddAzureKeyVault(
                        builtConfig["KeyVault:BaseUrl"],
                        client,
                        new DefaultKeyVaultSecretManager());
                })
                .UseStartup<Startup>()
                .Build(); 
    }
}
