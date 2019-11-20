// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AzureRenderHub.WebApp.Arm.Deploying;
using AzureRenderHub.WebApp.Providers.Logs;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Client.TokenCacheProviders;
using Microsoft.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using WebApp.AppInsights;
using WebApp.AppInsights.ActiveNodes;
using WebApp.AppInsights.PoolUsage;
using WebApp.Arm;
using WebApp.Authorization;
using WebApp.BackgroundHosts.AutoScale;
using WebApp.BackgroundHosts.Deployment;
using WebApp.BackgroundHosts.LeaseMaintainer;
using WebApp.BackgroundHosts.ScaleUpProcessor;
using WebApp.Code.Contract;
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

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None;
            });

            services.AddOptions();

            // Token acquisition service based on MSAL.NET
            // and chosen token cache implementation
            services.AddAzureAdV2Authentication(Configuration)
                    .AddMsal(new string[] { "https://graph.microsoft.com/.default" })
                    .AddInMemoryTokenCaches();

            services.AddSession(options =>
            {
                options.Cookie.Name = "RenderHub.Session";
                options.IdleTimeout = TimeSpan.FromMinutes(10);
            });

            services.AddTransient<IGraphProvider, GraphProvider>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
            });

            // Add Application Insights integration:
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddLogging(builder =>
            {
                builder.AddApplicationInsights();

                // Adding the filter below to ensure logs of all severity from Program.cs
                // is sent to ApplicationInsights.
                builder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>(
                    typeof(Program).FullName, Microsoft.Extensions.Logging.LogLevel.Trace);

                // Adding the filter below to ensure logs of all severity from Startup.cs
                // is sent to ApplicationInsights.
                builder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>(
                    typeof(Startup).FullName, Microsoft.Extensions.Logging.LogLevel.Trace);

                builder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>(
                    "WebApp", Microsoft.Extensions.Logging.LogLevel.Trace);

                builder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>(
                    "", Microsoft.Extensions.Logging.LogLevel.Error);
            });

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
            services.AddMemoryCache();

            // Add HttpClient with retry policy for contacting Cost Management
            services.AddHttpClient<CostManagementClientAccessor>();
                // -- This is to be restored when CostManagement returns a non-500 for invalid subs.
                ////.AddTransientHttpErrorPolicy(b =>
                ////    b.WaitAndRetryAsync(
                ////        new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) }));


            // These are scoped as they use the credentials of the user:
            services.AddScoped<ILogProvider, LogProvider>();
            services.AddScoped<IAzureResourceProvider, AzureResourceProvider>();
            services.AddScoped<IDeploymentCoordinator, DeploymentCoordinator>();
            services.AddScoped<AuthorizationManager>();
            services.AddScoped<ICostCoordinator>(p =>
            {
                var client = p.GetRequiredService<CostManagementClientAccessor>();
                var memoryCache = p.GetRequiredService<IMemoryCache>();
                return new CachingCostCoordinator(new CostCoordinator(client), memoryCache);
            });

            services.AddSingleton<IIdentityProvider, IdentityProvider>();
            services.AddSingleton<ManagementClientMsiProvider>();
            services.AddSingleton<StartTaskProvider>();

            services.AddSingleton<IEnvironmentCoordinator>(p =>
            {
                var cbc = p.GetRequiredService<CloudBlobClient>();
                var kvClient = p.GetRequiredService<IKeyVaultMsiClient>();
                var cache = p.GetRequiredService<IMemoryCache>();
                var logger = p.GetRequiredService<ILogger<EnvironmentSecretsRepository>>();

                // note that cache is around the secrets so they don't have to be re-fetched
                return
                    new EnvironmentCoordinator(
                        new CachingConfigRepository<RenderingEnvironment>(
                            new EnvironmentSecretsRepository(
                                new GenericConfigRepository<RenderingEnvironment>(
                                    cbc.GetContainerReference("environments")),
                                    kvClient,
                                    logger),
                            cache));
            });

            // the default IAssetRepoCoordinator implementation uses the user auth
            services.AddScoped<IManagementClientProvider, ManagementClientHttpContextProvider>();
            services.AddScoped(p =>
                CreateAssetRepoCoordinator(
                    p,
                    p.GetRequiredService<IManagementClientProvider>(),
                    p.GetRequiredService<IAzureResourceProvider>())); 

            services.AddSingleton<IPackageCoordinator>(p =>
            {
                var cbc = p.GetRequiredService<CloudBlobClient>();
                var cache = p.GetRequiredService<IMemoryCache>();
                return new PackageCoordinator(
                    new CachingConfigRepository<InstallationPackage>(
                        new GenericConfigRepository<InstallationPackage>(cbc.GetContainerReference("packages")),
                        cache));
            });

            services.AddScoped<IPoolCoordinator, PoolCoordinator>();
            services.AddSingleton<IScaleUpRequestStore, ScaleUpRequestStore>();

            services.AddScoped<IVMSizes, VMSizes>();
            services.AddScoped<IPoolUsageProvider, PoolUsageProvider>();

            // Deployment background server
            services.AddSingleton<ITemplateProvider, TemplateProvider>();
            services.AddSingleton<ILeaseMaintainer, LeaseMaintainer>();
            services.AddSingleton<IDeploymentQueue, DeploymentQueue>();
            services.AddSingleton<IHostedService>(p => new BackgroundDeploymentHost(
                // use MSI auth for background services, do not provide an IAzureResourceProvider implementation
                CreateAssetRepoCoordinator(p, p.GetRequiredService<ManagementClientMsiProvider>(), null),
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

        private static IAssetRepoCoordinator CreateAssetRepoCoordinator(
            IServiceProvider p,
            IManagementClientProvider clientProvider,
            IAzureResourceProvider resourceProvider)
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
                clientProvider,
                resourceProvider,
                p.GetRequiredService<ILogger<AssetRepoCoordinator>>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            app.UseExceptionHandler("/Environments/Error");
            app.UseHsts();
            app.UseHttpsRedirection();

            // set a longer cacheability for static assets
            var pathsToCache = new[] { "/css", "/js", "/images", "/lib" };
            app.UseStaticFiles(
                new StaticFileOptions
                {
                    OnPrepareResponse =
                        ctx =>
                        {
                            var path = ctx.Context.Request.Path;
                            foreach (var shouldBeCached in pathsToCache)
                            {
                                if (path.StartsWithSegments(shouldBeCached))
                                {
                                    var headers = ctx.Context.Response.GetTypedHeaders();
                                    headers.CacheControl =
                                        new CacheControlHeaderValue
                                        {
                                            Public = true,
                                            MaxAge = TimeSpan.FromDays(30),
                                        };

                                    break;
                                }
                            }
                        }
                });

            app.UseSession();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
