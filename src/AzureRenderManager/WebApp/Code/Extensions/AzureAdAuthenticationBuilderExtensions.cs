// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

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

                //options.Events.OnAuthorizationCodeReceived += async context =>
                //{
                //    var userId = context.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

                //    var authContext = new AuthenticationContext(context.Options.Authority);
                //    var clientCred = new ClientCredential(context.Options.ClientId, context.Options.ClientSecret);

                //    AuthenticationResult authResult;
                //    try
                //    {
                //        authResult =
                //            await authContext.AcquireTokenSilentAsync(
                //                context.Options.Resource,
                //                clientCred,
                //                new UserIdentifier(userId, UserIdentifierType.UniqueId));
                //    }
                //    catch (AdalSilentTokenAcquisitionException)
                //    {
                //        authResult =
                //            await authContext.AcquireTokenByAuthorizationCodeAsync(
                //                context.TokenEndpointRequest.Code,
                //                new Uri(context.TokenEndpointRequest.RedirectUri, UriKind.RelativeOrAbsolute),
                //                clientCred,
                //                context.Options.Resource);
                //    }

                //    context.HandleCodeRedemption(authResult.AccessToken, context.ProtocolMessage.IdToken);
                //};
            }

            public void Configure(OpenIdConnectOptions options)
            {
                Configure(Options.DefaultName, options);
            }
        }
    }
}
