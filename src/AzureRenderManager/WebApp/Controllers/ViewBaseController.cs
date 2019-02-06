// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using WebApp.Code.Attributes;

namespace WebApp.Controllers
{
    [RequireConfig]
    public class ViewBaseController : BaseController
    {
        protected async Task<TokenCredentials> GetTokenCredentials()
        {
            var accessToken = await GetAccessToken();
            return new TokenCredentials(accessToken);
        }

        protected async Task<ResourceManagementClient> GetResourceClient(string subscriptionId)
        {
            var token = await GetTokenCredentials();
            return new ResourceManagementClient(token) { SubscriptionId = subscriptionId };
        }

        protected async Task<NetworkManagementClient> GetNetworkManagementClient(string subscriptionId)
        {
            var token = await GetTokenCredentials();
            return new NetworkManagementClient(token) { SubscriptionId = subscriptionId };
        }

        protected async Task<bool> ValidateResourceGroup(ResourceManagementClient client, string rgName)
        {
            try
            {
                // check the resource group name is unique. this will change.
                var currentResourceGroup = await client.ResourceGroups.GetAsync(rgName);
                if (currentResourceGroup != null)
                {
                    // NewResourceGroupName ties to the name in the model, change both if needed
                    ModelState["NewResourceGroupName"].ValidationState = ModelValidationState.Invalid;
                    ModelState["NewResourceGroupName"].Errors.Add("The Resource Group name currently exists in the associated Azure Subscription.");

                    return false;
                }
            }
            catch (CloudException cEx)
            {
                if (cEx.Response.StatusCode == HttpStatusCode.NotFound || cEx.Body.Code == "NotFound")
                {
                    // Pass - RG doesn't exist, and that's what we want
                    return true;
                }
                else
                {
                    // TODO: Log or do something else ...
                }
            }

            // TODO: This should set another error and return false ...
            return true;
        }
    }
}
