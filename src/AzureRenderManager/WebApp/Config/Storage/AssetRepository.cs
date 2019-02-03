// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

        public string SubscriptionId { get; set; }

        public Subnet Subnet { get; set; }

        public string ProvisioningState { get; set; }

        public string ResourceGroupName { get; set; }

        public string ResourceGroupResourceId => $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}";

        public string DeploymentName { get; set; }

        public bool InProgress { get; set; }

        // from ISubMenuItem

        public virtual string Id => Name;

        public virtual string DisplayName => Name;

        public bool Enabled => !InProgress;

        /// <summary>
        /// TODO: Don't really like the domain model knowing about the view model, so
        /// we can create another way of translating these things. Maybe implement an
        /// injectable IMapper to translate entities
        /// </summary>
        /// <param name="model"></param>
        public abstract void UpdateFromModel(AddAssetRepoBaseModel model);
    }
}
