// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using WebApp.Code;
using WebApp.Config;
using WebApp.Config.Storage;

namespace WebApp.Models.Storage.Create
{
    public class AddNetworkingModel : AddAssetRepoBaseModel
    {
        private const string DefaultStorageSubnetName = "storage-subnet";

        // needs this empty constructor for model bindings
        public AddNetworkingModel()
        {
            RepositoryType = AssetRepositoryType.NfsFileServer;
        }

        public AddNetworkingModel(AssetRepository storage, List<Subnet> existingSubnets)
        {
            RepositoryName = storage.Name;
            RepositoryType = storage.RepositoryType;
            VirtualNetworkName = storage.SelectedVNet?.Name;
            VirtualNetworkAddressPrefixes = storage.SelectedVNet?.AddressPrefixes;
            ExistingSubnets = existingSubnets;
            NewSubnetName = FindNewSubnetName(existingSubnets);
            CreateSubnet = storage.Subnet == null;
            Subnet = storage.Subnet;
        }

        private string FindNewSubnetName(List<Subnet> existingSubnets)
        {
            if (existingSubnets == null
                || existingSubnets.Count == 0
                || !existingSubnets.Any(
                    subnet => subnet.Name.Equals(DefaultStorageSubnetName, StringComparison.InvariantCultureIgnoreCase)))
            {
                return DefaultStorageSubnetName;
            }

            var index = 1;
            while (true)
            {
                var subnetName = $"{DefaultStorageSubnetName}-{index++}";
                if (!existingSubnets.Any(subnet => subnet.Name.Equals(subnetName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return subnetName;
                }
            }
        }

        public string VirtualNetworkName { get; set; }

        public string VirtualNetworkAddressPrefixes { get; set; }

        public List<Subnet> ExistingSubnets { get; set; }

        public string SerializedSubnetList
        {
            get
            {
                if (ExistingSubnets == null || ExistingSubnets.Count == 0)
                {
                    return null;
                }
                return string.Join('|', ExistingSubnets.Select(subnet => subnet.ToString()));
            }

            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    ExistingSubnets = value.Split('|').Select(s => new Subnet(s)).ToList();
                }
            }
        }

        public List<SelectListItem> ExistingSubnetsSelectList => 
            ExistingSubnets?.Select(SubnetToSelectListItem).ToList();

        private SelectListItem SubnetToSelectListItem(Subnet subnet)
        {
            // If Subnet != null then an existing subnet has already been created for the repo
            var selected = Subnet != null && Subnet.Name.Equals(subnet.Name, StringComparison.InvariantCultureIgnoreCase);
            return new SelectListItem(subnet.Name, subnet.ToString(), selected);
        }

        public bool CreateSubnet { get; set; }

        [RegularExpression(Validation.RegularExpressions.ResourceGroup, ErrorMessage = Validation.Errors.Regex.Subnet)]
        [StringLength(Validation.MaxLength.ResourceGroupName)]
        public string NewSubnetName { get; set; }

        // e.g. CIDR 10.2.0.0/24
        [RegularExpression(Validation.RegularExpressions.CidrSubnetRange, ErrorMessage = Validation.Errors.Regex.SubnetAddressRange)]
        public string NewSubnetAddressPrefix { get; set; }
    }
}
