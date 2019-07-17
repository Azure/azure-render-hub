// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using WebApp.Code;
using WebApp.Config;
using WebApp.Config.Storage;

namespace WebApp.Models.Storage.Create
{
    public abstract class AddAssetRepoBaseModel
    {
        [Required(ErrorMessage = "Storage configuration name is a required field")]
        [RegularExpression(Validation.RegularExpressions.AssetRepoName, ErrorMessage = Validation.Errors.Regex.AssetRepoName)]
        public string RepositoryName { get; set; }

        [Required]
        [EnumDataType(typeof(AssetRepositoryType))]
        public AssetRepositoryType RepositoryType { get; set; }

        public Subnet Subnet { get; set; }

        /// <summary>
        /// Semi-colon delimited resource id, location and address prefix
        /// </summary>
        public string SubnetResourceIdLocationAndAddressPrefix => Subnet?.ToString();

        public bool UseEnvironment { get; set; } = true;

        public List<string> Environments { get; set; }

        public string SelectedEnvironmentName { get; set; }

        [Required]
        public Guid? SubscriptionId { get; set; }

        public string Error { get; set; }

        public string ErrorMessage { get; set; }
    }
}
