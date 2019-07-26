// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Identity.Web.Client;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using WebApp.Code.Attributes;

namespace WebApp.Controllers
{
    [RequireConfig]
    public class ViewBaseController : BaseController
    {
        public ViewBaseController(ITokenAcquisition tokenAcquisition) : base(tokenAcquisition)
        {
        }

        protected async Task<bool> ValidateResourceGroup(
            IResourceManagementClient client, 
            string resourceGroupName,
            string modelPropertyName)
        {
            // check the resource group name is unique. this will change.
            if (await client.ResourceGroups.CheckExistenceAsync(resourceGroupName))
            {
                var resources = (await client.Resources.ListByResourceGroupAsync(resourceGroupName)).ToList();

                if (resources.Any())
                {
                    // NewResourceGroupName ties to the name in the model, change both if needed
                    ModelState[modelPropertyName].ValidationState = ModelValidationState.Invalid;
                    ModelState[modelPropertyName].Errors.Add("The Resource Group name currently exists in the associated Azure Subscription.");

                    return false;
                }
            }

            return true;
        }
    }
}
