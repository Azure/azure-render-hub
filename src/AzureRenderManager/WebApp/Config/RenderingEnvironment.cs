// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WebApp.Config.RenderManager;
using WebApp.Models;

namespace WebApp.Config
{
    public class RenderingEnvironment : ISubMenuItem
    {
        private string _envName = "";

        public RenderingEnvironment()
        {
            AutoScaleConfiguration = new AutoScaleConfiguration();
            State = EnvironmentState.Creating;
        }

        // TODO: Need a proper ID
        public string Id => Name;

        public string DisplayName => Name;

        public string Name {
            get => _envName;
            set
            {
                _envName = value;
                ResourceGroupName = Name + "-rg";
            }
        }

        public EnvironmentState State { get; set; }

        public string ResourceGroupName { get; set; }
        
        public string ResourceGroupResourceId => $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}";

        // The full name, like "West US"
        public string LocationName { get; set; }

        public Guid SubscriptionId { get; set; }

        public KeyVault KeyVault { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public RenderManagerType RenderManager { get; set; }

        public ServicePrincipal KeyVaultServicePrincipal { get; set; }

        public Subnet Subnet { get; set; }

        public BatchAccount BatchAccount { get; set; }

        public StorageAccount StorageAccount { get; set; }

        public ApplicationInsightsAccount ApplicationInsightsAccount { get; set; }

        public RenderManagerConfig RenderManagerConfig { get; set; }

        public bool InProgress { get; set; }

        public bool Enabled => !InProgress;

        public DomainConfig Domain { get; set; }

        public AutoScaleConfiguration AutoScaleConfiguration { get; set; }

        public DeletionSettings DeletionSettings { get; set; }

        public string WindowsBootstrapScript { get; set; }

        public string LinuxBootstrapScript { get; set; }
    }

    public enum EnvironmentState
    {
        Creating,
        Steady,
        Deleting,
        DeleteFailed
    }
}
