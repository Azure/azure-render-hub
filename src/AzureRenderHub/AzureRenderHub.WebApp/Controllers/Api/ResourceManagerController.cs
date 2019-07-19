// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.ApplicationInsights.Management;
using Microsoft.Azure.Management.ApplicationInsights.Management.Models;
using Microsoft.Azure.Management.Batch;
using Microsoft.Azure.Management.Batch.Models;
using Microsoft.Azure.Management.Consumption;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web.Client;
using Microsoft.Rest;
using WebApp.Code;
using WebApp.Code.Extensions;
using WebApp.Models.Api;

namespace WebApp.Controllers.Api
{
    public class ResourceManagerController : BaseController
    {
        private readonly IConfiguration _configuration;

        public ResourceManagerController(
            IConfiguration configuration,
            ITokenAcquisition tokenAcquisition) : base(tokenAcquisition)
        {
            _configuration = configuration;
        }

        [Route("api/subscriptions"), HttpGet]
        public async Task<List<AzureSubscription>> GetSubscriptions()
        {
            // look for them in in session cache first
            var cached = HttpContext.Session.Get<List<AzureSubscription>>(CacheKeys.SubscriptionList);
            if (cached != null)
            {
                return cached;
            }

            var tenantId = _configuration.GetSection("AzureAd:TenantId").Value;
            var accessToken = await GetAccessToken();

            using (var client = new SubscriptionClient(new TokenCredentials(accessToken)))
            {
                var subs = (await client.Subscriptions.ListAsync())
                .Where(sub => sub.State == SubscriptionState.Enabled)
                .OrderBy(sub => sub.DisplayName.ToUpperInvariant())
                .Select(sub => new AzureSubscription(sub))
                .ToList();

                // cache them for next time
                return HttpContext.Session.Set(CacheKeys.SubscriptionList, subs);
            }
        }

        [Route("api/subscriptions/{subscriptionId}/batchaccounts/{location?}"), HttpGet]
        public async Task<List<BatchAccount>> GetBatchAccounts(string subscriptionId, string location)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var batchClient = new BatchManagementClient(token) { SubscriptionId = subscriptionId };
            var batchAccounts = await batchClient.BatchAccount.ListAsync();

            return batchAccounts
                .Where(acc => string.IsNullOrWhiteSpace(location) || acc.Location.Equals(location, StringComparison.OrdinalIgnoreCase))
                .OrderBy(acc => acc.Name.ToUpperInvariant())
                .ToList();
        }

        [Route("api/subscriptions/{subscriptionId}/storageaccounts/{location?}"), HttpGet]
        public async Task<List<StorageAccount>> GetStorageAccounts(string subscriptionId, string location)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var storageClient = new StorageManagementClient(token) { SubscriptionId = subscriptionId };
            var storageAccounts = await storageClient.StorageAccounts.ListAsync();

            return storageAccounts
                .Where(acc => string.IsNullOrWhiteSpace(location) || acc.Location.Equals(location, StringComparison.OrdinalIgnoreCase))
                .OrderBy(acc => acc.Name)
                .ToList();
        }


        [Route("api/subscriptions/{subscriptionId}/subnets/{location?}"), HttpGet]
        public async Task<List<AzureSubnet>> GetSubnets(string subscriptionId, string location)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var networkClient = new NetworkManagementClient(token) { SubscriptionId = subscriptionId };
            var vNets = await networkClient.VirtualNetworks.ListAllAsync();

            return vNets
                .Where(vnet => string.IsNullOrWhiteSpace(location) || vnet.Location.Equals(location, StringComparison.OrdinalIgnoreCase))
                .SelectMany(vnet => vnet.Subnets.Select(subnet => new AzureSubnet(vnet, subnet)))
                .OrderBy(subnet => subnet.Name)
                .ToList();
        }

        [Route("api/subscriptions/{subscriptionId}/applicationinsights"), HttpGet]
        public async Task<List<ApplicationInsightsComponent>> GetApplicationInsights(string subscriptionId)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var appInsightsClient = new ApplicationInsightsManagementClient(token) { SubscriptionId = subscriptionId };
            var components = await appInsightsClient.Components.ListWithHttpMessagesAsync();

            return components.Body.OrderBy(ins => ins.Name).ToList();
        }

        [Route("api/subscriptions/{subscriptionId}/resourcegroups"), HttpGet]
        public async Task<List<ResourceGroup>> GetResourceGroups(string subscriptionId)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var resourceClient = new ResourceManagementClient(token) { SubscriptionId = subscriptionId };
            var rgs = await resourceClient.ResourceGroups.ListAsync();

            return rgs.ToList();
        }

        [Route("api/subscriptions/{subscriptionId}/resourcegroups/{rgName}"), HttpGet]
        public async Task<List<GenericResource>> ListResourceGroupResources(string subscriptionId, string rgName)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var resourceClient = new ResourceManagementClient(token) { SubscriptionId = subscriptionId };
            var resources = await resourceClient.Resources.ListByResourceGroupAsync(rgName);

            return resources.ToList();
        }

        [Route("api/subscriptions/{subscriptionId}/locations"), HttpGet]
        public async Task<ActionResult> GetLocations(string subscriptionId)
        {
            var locations = await GetLocationsForSubscription(subscriptionId);
            if (locations == null)
            {
                return NotFound();
            }
            return Ok(locations);
        }

        // i.e. get locations for Microsoft.Insights i
        [Route("api/subscriptions/{subscriptionId}/resourceProvider/{resourceProviderNamespace}/resourceType/{resourceType}/locations"), HttpGet]
        public async Task<ActionResult> GetProviderLocations(string subscriptionId, string resourceProviderNamespace, string resourceType)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var resourceClient = new ResourceManagementClient(token) { SubscriptionId = subscriptionId };
            var providers = await resourceClient.Providers.ListAsync();
            var locationNames = providers
                .FirstOrDefault(p =>
                    p.NamespaceProperty.Equals(resourceProviderNamespace, StringComparison.InvariantCultureIgnoreCase))
                ?.ResourceTypes.FirstOrDefault(r =>
                    r.ResourceType.Equals(resourceType, StringComparison.InvariantCultureIgnoreCase))
                ?.Locations;

            if (locationNames == null)
            {
                return NotFound();
            }

            var locations = await GetLocationsForSubscription(subscriptionId);

            return Ok(locations.Where(l => locationNames.Contains(l.DisplayName)));
        }

        [Route("api/subscriptions/{subscriptionId}/usage"), HttpGet]
        public async Task<ActionResult> GetUsage(string subscriptionId)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var costClient = new ConsumptionManagementClient(token) { SubscriptionId = subscriptionId };
            var usage = await costClient.UsageDetails.ListAsync("properties/additionalProperties");
            return Ok(usage);
        }

        private async Task<List<Location>> GetLocationsForSubscription(string subscriptionId)
        {
            // look for them in in session cache first
            var cached = HttpContext.Session.Get<List<Location>>(CacheKeys.LocationList);
            if (cached != null)
            {
                return cached;
            }

            var tenantId = _configuration.GetSection("AzureAd:TenantId").Value;
            var accessToken = await GetAccessToken();
            using (var client = new SubscriptionClient(new TokenCredentials(accessToken)))
            {
                var sub = await client.Subscriptions.GetAsync(subscriptionId);
                if (sub == null)
                {
                    return null;
                }

                var locations =
                    (await client.Subscriptions.ListLocationsAsync(subscriptionId))
                    .OrderBy(l => l.Name).ToList();

                // cache them for next time
                return HttpContext.Session.Set(CacheKeys.LocationList, locations);
            }
        }

        [Route("api/subscriptions/{subscriptionId}/keyvaults"), HttpGet]
        public async Task<List<Vault>> GetKeyVaults(string subscriptionId)
        {
            var accessToken = await GetAccessToken();
            var token = new TokenCredentials(accessToken);
            var keyVaultClient = new KeyVaultManagementClient(token) { SubscriptionId = subscriptionId };

            var vaults = new List<Vault>();

            var vaultsResponse = await keyVaultClient.Vaults.ListBySubscriptionAsync();
            vaults.AddRange(vaultsResponse);

            while (!string.IsNullOrEmpty(vaultsResponse.NextPageLink))
            {
                vaultsResponse = await keyVaultClient.Vaults.ListBySubscriptionNextAsync(vaultsResponse.NextPageLink);
                vaults.AddRange(vaultsResponse);
            }

            return vaults.OrderBy(kv => kv.Name).ToList();
        }
    }
}