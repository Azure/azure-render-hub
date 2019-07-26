// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebApp.Code.Validators;
using WebApp.Config.Resources;
using WebApp.Config.Storage;

namespace WebApp.Models.Storage
{
    public class DeleteStorageModel
    {
        // needs this empty constructor for model bindings
        public DeleteStorageModel()
        {
            Resources = new List<GenericResource>();
        }

        public DeleteStorageModel(AssetRepository storage, List<GenericResource> resources = null)
        {
            // default to deleting the resource group
            // TODO: add a flag to the env to verify if customer or us created the RG. 
            Resources = resources ?? new List<GenericResource>();

            if (storage != null)
            {
                Name = storage.Name;
                SubscriptionId = storage.SubscriptionId;
                LocationName = storage.Subnet.Location;
                ResourceGroup = storage.ResourceGroupName;
                DeleteResourceGroup = !string.IsNullOrEmpty(ResourceGroup);
            }
        }

        // Details
        public string Name { get; set; }

        public Guid? SubscriptionId { get; set; }

        public string LocationName { get; set; }

        public string ResourceGroup { get; set; }

        public bool HasResourceGroup => false == string.IsNullOrEmpty(ResourceGroup);

        [Required(ErrorMessage = "Please enter the environment name to confirm delete.")]
        [ConfirmDeletion(ErrorMessage = "The entered name must match the current environment name.")]
        public string Confirmation { get; set; }

        public List<GenericResource> Resources { get; }

        public int ResourceCount => Resources.Count;

        public bool ResourceLoadFailed { get; set; }

        public bool DeleteResourceGroup { get; set; }

        public bool DeleteVirtualMachines { get; set; }
    }
}
