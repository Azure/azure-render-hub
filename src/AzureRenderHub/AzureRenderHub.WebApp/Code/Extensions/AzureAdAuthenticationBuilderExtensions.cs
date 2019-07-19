// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using WebApp.Code.Session;

namespace WebApp.Code.Extensions
{
    public static class AzureAdAuthenticationBuilderExtensions
    {        
        public static AuthenticationBuilder AddAzureAd(this AuthenticationBuilder builder)
            => builder.AddAzureAd(_ => { });

        public static AuthenticationBuilder AddAzureAd(this AuthenticationBuilder builder, Action<AzureAdOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureAzureOptions>();
            builder.AddOpenIdConnect();
            return builder;
        }

        private class ConfigureAzureOptions : IConfigureNamedOptions<OpenIdConnectOptions>
        {
            private readonly AzureAdOptions _azureOptions;

            public ConfigureAzureOptions(IOptions<AzureAdOptions> azureOptions)
            {
                _azureOptions = azureOptions.Value;
            }

            public void Configure(string name, OpenIdConnectOptions options)
            {
                options.Authority = $"{_azureOptions.Instance}{_azureOptions.TenantId}";
                options.ClientId = _azureOptions.ClientId;
                options.ClientSecret = _azureOptions.ClientSecret;
                options.CallbackPath = _azureOptions.CallbackPath;
                options.UseTokenLifetime = true;
                options.RequireHttpsMetadata = true;
                options.Resource = "https://management.azure.com/";

                options.ResponseType = OpenIdConnectResponseType.CodeIdToken; // do token-based auth
                options.SaveTokens = true; // save access_token where we can access it

                // trying to get refresh to work
                options.Scope.Add("offline_access"); // allow refreshing

                options.Events = new OpenIdConnectEvents
                {
                    OnAuthorizationCodeReceived = async ctx =>
                    {
                        var request = ctx.HttpContext.Request;
                        var currentUri = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path);
                        var credential = new Microsoft.IdentityModel.Clients.ActiveDirectory.ClientCredential(ctx.Options.ClientId, ctx.Options.ClientSecret);

                        var memoryCache = ctx.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                        var cache = new SessionTokenCache(ctx.Principal, memoryCache);
                        var tokenCache = cache.GetCacheInstance();
                        var authContext = new AuthenticationContext(ctx.Options.Authority, true, tokenCache);

                        var result = await authContext.AcquireTokenByAuthorizationCodeAsync(
                            ctx.ProtocolMessage.Code, new Uri(currentUri), credential, ctx.Options.Resource);

                        ctx.HandleCodeRedemption(result.AccessToken, result.IdToken);
                    }
                };
            }

            public void Configure(OpenIdConnectOptions options)
            {
                Configure(Options.DefaultName, options);
            }
        }
    }
}
