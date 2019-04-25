// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.ComponentModel.DataAnnotations;

using WebApp.Code;
using WebApp.Code.Extensions;
using WebApp.Config;
using WebApp.Models.Environments.Create.Network;

namespace WebApp.Models.Environments.Create
{
    public class AddEnvironmentStep2Model : EnvironmentBaseModel
    {
        // needs this empty constructor for model bindings
        public AddEnvironmentStep2Model() { }

        public AddEnvironmentStep2Model(RenderingEnvironment environment)
        {
            if (environment != null)
            {
                EditMode = true;
                EnvironmentName = environment.Name;
                SubscriptionId = environment.SubscriptionId;
                RenderManager = environment.RenderManager;
                LocationName = environment.LocationName;
                NetworkSettingsLocked = (environment.ApplicationInsightsAccount != null ||
                                        environment.KeyVault != null ||
                                        environment.BatchAccount != null ||
                                        environment.StorageAccount != null);
            }
        }

        [Required]
        public Guid? SubscriptionId { get; set; }

        // When resources have been created in an 'in progress' env the
        // subscription Id cannot bechanged without deleting and restarting.
        public bool NetworkSettingsLocked { get; set; }

        [Required]
        public string LocationName { get; set; }

        [Required]
        [EnumDataType(typeof(RenderManagerType))]
        public new RenderManagerType? RenderManager { get; set; }

        [Required]
        [EnumDataType(typeof(AzureConnectionType))]
        public AzureConnectionType? AzureConnectionType { get; set; }

        public string SubnetResourceIdLocationAndAddressPrefix { get; set; }

        public VpnSettings VpnSettings { get; set; }

        // Required for VNet and VPN
        public VNetSettings VNetSettings { get; set; }
    }
}
