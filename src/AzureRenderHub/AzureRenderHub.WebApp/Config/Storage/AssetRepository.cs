// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WebApp.Models;
using WebApp.Models.Storage.Create;

namespace WebApp.Config.Storage
{
    public abstract class AssetRepository : ISubMenuItem
    {
        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public AssetRepositoryType RepositoryType { get; set; }

        public Guid SubscriptionId { get; set; }

        public string EnvironmentName { get; set; }

        public Subnet Subnet { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ProvisioningState ProvisioningState { get; set; }

        public string ResourceGroupName { get; set; }

        public string ResourceGroupResourceId => $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}";

        public string DeploymentName { get; set; }

        public string DeploymentResourceId => $"https://ms.portal.azure.com/#blade/HubsExtension/DeploymentDetailsBlade/overview/id/%2Fsubscriptions%2F82acd5bb-4206-47d4-9c12-a65db028483d%2FresourceGroups%2FAvere-christis-3%2Fproviders%2FMicrosoft.Resources%2Fdeployments%2FAvere-dd55fa6e-184e-4130-acf0-1772c3af298d";

        public string DeploymentLink => $"https://ms.portal.azure.com/#blade/HubsExtension/DeploymentDetailsBlade/overview/id/%2Fsubscriptions%2F82acd5bb-4206-47d4-9c12-a65db028483d%2FresourceGroups%2FAvere-christis-3%2Fproviders%2FMicrosoft.Resources%2Fdeployments%2FAvere-dd55fa6e-184e-4130-acf0-1772c3af298d";

        public bool InProgress { get; set; }

        /// <summary>
        /// TODO: Don't really like the domain model knowing about the view model, so
        /// we can create another way of translating these things. Maybe implement an
        /// injectable IMapper to translate entities
        /// </summary>
        /// <param name="model"></param>
        public abstract void UpdateFromModel(AddAssetRepoBaseModel model);

        public abstract string GetTemplateName();

        public abstract Dictionary<string, object> GetTemplateParameters();

        // from ISubMenuItem
        public virtual string Id => Name;

        public virtual string DisplayName => Name;

        public bool Enabled => !InProgress;
        // end ISubMenuItem
    }

    public enum ProvisioningState
    {
        Unknown,
        Creating, // Config creted, not deploying yet
        Running, // ARM deployment
        Succeeded, // Deployed and ready
        Failed, // Something failed in the deployment
        Deleting // Deleting
    }
}
