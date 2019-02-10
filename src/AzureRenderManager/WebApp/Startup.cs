// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using WebApp.AppInsights;
using WebApp.AppInsights.ActiveNodes;
using WebApp.AppInsights.PoolUsage;
using WebApp.Arm;
using WebApp.BackgroundHosts.AutoScale;
using WebApp.BackgroundHosts.Deployment;
using WebApp.BackgroundHosts.LeaseMaintainer;
using WebApp.BackgroundHosts.ScaleUpProcessor;
using WebApp.Code.Contract;
using WebApp.Code.Extensions;
using WebApp.Config;
using WebApp.Config.Coordinators;
using WebApp.Config.Pools;
using WebApp.Config.Storage;
using WebApp.CostManagement;
using WebApp.Identity;
using WebApp.Operations;
using WebApp.Providers.Resize;
using WebApp.Providers.Templates;
using WebApp.Util;

namespace WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration, Microsoft.AspNetCore.Hosting.IHostingEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public Microsoft.AspNetCore.Hosting.IHostingEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })

            .AddAzureAd(options => Configuration.Bind("AzureAd", options))
            .AddCookie(
                options =>
                {
                    options.Events.OnValidatePrincipal =
                        context =>
                        {
                            if (context.Properties.Items.TryGetValue(".Token.expires_at", out var expiryString)
                                && DateTimeOffset.TryParse(expiryString, out var expiresAt)
                                && expiresAt < DateTimeOffset.UtcNow)
                            {
                                // TODO: we should be able to refresh this without redirecting the user...
                                context.RejectPrincipal();
                            }

                            return Task.CompletedTask;
                        };
                });

            // Session state cache
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.Cookie.Name = "RenderFarmManager.Session";
                options.IdleTimeout = TimeSpan.FromMinutes(10);
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
            });

            // Add Application Insights integration:
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddLogging(c => c.AddApplicationInsights());

            services.AddSingleton(
                p =>
                {
                    var config = p.GetRequiredService<IConfiguration>();
                    var secretName = config["KeyVault:StorageConnectionStringSecretName"];
                    var connectionString = config[secretName];
                    return CloudStorageAccount.Parse(connectionString);
                });

            services.AddSingleton(p => p.GetRequiredService<CloudStorageAccount>().CreateCloudBlobClient());
            services.AddSingleton(p => p.GetRequiredService<CloudStorageAccount>().CreateCloudQueueClient());
            services.AddSingleton(p => p.GetRequiredService<CloudStorageAccount>().CreateCloudTableClient());
            services.AddSingleton<IKeyVaultMsiClient, KeyVaultMsiClient>();

            services.AddSingleton<BatchClientMsiProvider>();
            services.AddSingleton(Environment);

            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddMemoryCache();

            // These are scoped as they use the credentials of the user:
            services.AddHttpClient<CostManagementClientAccessor>(); // register for CostManagementClientAccessor
            services.AddScoped<IAzureResourceProvider, AzureResourceProvider>();

            services.AddSingleton<IIdentityProvider, IdentityProvider>();
            services.AddSingleton<IManagementClientProvider, ManagementClientMsiProvider>();
            services.AddSingleton<ManagementClientMsiProvider>();
            services.AddSingleton<StartTaskProvider>();

            services.AddSingleton<IEnvironmentCoordinator>(p =>
            {
                var cbc = p.GetRequiredService<CloudBlobClient>();
                var kvClient = p.GetRequiredService<IKeyVaultMsiClient>();
                var cache = p.GetRequiredService<IMemoryCache>();

                // note that cache is around the secrets so they don't have to be re-fetched
                return
                    new EnvironmentCoordinator(
                        new CachingConfigRepository<RenderingEnvironment>(
                            new EnvironmentSecretsRepository(
                                new GenericConfigRepository<RenderingEnvironment>(
                                    cbc.GetContainerReference("environments")),
                                    kvClient),
                            cache));
            });

            services.AddSingleton<IAssetRepoCoordinator>(p =>
            {
                var cbc = p.GetRequiredService<CloudBlobClient>();
                var cache = p.GetRequiredService<IMemoryCache>();
                return new AssetRepoCoordinator(
                    new CachingConfigRepository<AssetRepository>(
                        new GenericConfigRepository<AssetRepository>(cbc.GetContainerReference("storage")), 
                        cache),
                    p.GetRequiredService<ITemplateProvider>(),
                    p.GetRequiredService<IIdentityProvider>(),
                    p.GetRequiredService<IDeploymentQueue>(),
                    p.GetRequiredService<ILogger<AssetRepoCoordinator>>());
            });

            services.AddSingleton<IPackageCoordinator>(p =>
            {
                var cbc = p.GetRequiredService<CloudBlobClient>();
                var cache = p.GetRequiredService<IMemoryCache>();
                return new PackageCoordinator(
                    new CachingConfigRepository<InstallationPackage>(
                        new GenericConfigRepository<InstallationPackage>(cbc.GetContainerReference("packages")),
                        cache));
            });

            services.AddScoped<IManagementClientProvider, ManagementClientHttpContextProvider>();
            services.AddScoped<IPoolCoordinator, PoolCoordinator>();
            services.AddSingleton<IScaleUpRequestStore, ScaleUpRequestStore>();

            // While this does use credentials of the user, the data is not sensitive
            // and can be shared by multiple users:
            services.AddScoped<IVMSizes, VMSizes>();

            services.AddScoped<IPoolUsageProvider, PoolUsageProvider>();

            // Deployment background server
            services.AddSingleton<ITemplateProvider, TemplateProvider>();
            services.AddSingleton<ILeaseMaintainer, LeaseMaintainer>();
            services.AddSingleton<IDeploymentQueue, DeploymentQueue>();
            services.AddSingleton<IHostedService>(p => new BackgroundDeploymentHost(
                p.GetRequiredService<IAssetRepoCoordinator>(),
                p.GetRequiredService<ManagementClientMsiProvider>(),
                p.GetRequiredService<IDeploymentQueue>(),
                p.GetRequiredService<ILeaseMaintainer>(),
                p.GetRequiredService<ILogger<BackgroundDeploymentHost>>()));
            services.AddSingleton<IHostedService, AutoScaleHost>();
            services.AddSingleton<IAppInsightsQueryProvider, AppInsightsQueryProvider>();
            services.AddSingleton<IActiveNodeProvider, ActiveNodeProvider>();

            // Add scale up background service

            // Note that there is one AutoResetEvent that is used to communicate between the
            // ScaleController and the ScaleUpProcessorHost. If we want to use this type between
            // more instances then we will need to add some named-lookup system...
            services.AddSingleton<AsyncAutoResetEvent>();
            services.AddSingleton<IHostedService, ScaleUpProcessorHost>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
