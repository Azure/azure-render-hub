// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.ComponentModel.DataAnnotations;

using WebApp.Code;
using WebApp.Code.Extensions;
using WebApp.Config;

namespace WebApp.Models.Environments.Create
{
    public class AddEnvironmentStep1Model : EnvironmentBaseModel
    {
        // needs this empty constructor for model bindings
        public AddEnvironmentStep1Model() { }

        public AddEnvironmentStep1Model(RenderingEnvironment environment)
        {
            if (environment != null)
            {
                EditMode = true;
                OriginalName = environment.Name;
                EnvironmentName = environment.Name;
                SubscriptionId = environment.SubscriptionId;
                RenderManager = environment.RenderManager;
                LocationName = environment.LocationName;
                SubscriptionIdLocked = (environment.ApplicationInsightsAccount != null ||
                                        environment.KeyVault != null ||
                                        environment.BatchAccount != null ||
                                        environment.StorageAccount != null);
            }
        }

        /// <summary>
        /// In the form, keep hold of the initially set name in case we change it.
        /// </summary>
        public string OriginalName { get; set; }

        [Required]
        public Guid? SubscriptionId { get; set; }

        // When resources have been created in an 'in progress' env the
        // subscription Id cannot bechanged without deleting and restarting.
        public bool SubscriptionIdLocked { get; set; }

        [Required]
        public string LocationName { get; set; }

        [Required]
        [EnumDataType(typeof(RenderManagerType))]
        public new RenderManagerType? RenderManager { get; set; }
    }
}
