// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using WebApp.Code;
using WebApp.Code.Validators;
using WebApp.Config.Storage;

namespace WebApp.Models.Storage.Create
{
    public class AddNfsFileServerModel : AddAssetRepoBaseModel
    {
        // needs this empty constructor for model bindings
        public AddNfsFileServerModel()
        {
            RepositoryType = AssetRepositoryType.NfsFileServer;
        }

        public AddNfsFileServerModel(NfsFileServer fileServer)
        {
            RepositoryName = fileServer.Name;
            RepositoryType = fileServer.RepositoryType;
            NewResourceGroupName = fileServer.ResourceGroupName;
            SubscriptionId = fileServer.SubscriptionId;
            Subnet = fileServer.Subnet;
            AllowedNetworks = Subnet?.AddressPrefix;
            VmName = fileServer.VmName;
            VmSize = fileServer.VmSize;
            UserName = fileServer.Username;
        }

        [Required(ErrorMessage = Validation.Errors.Required.ResourceGroup)]
        [RegularExpression(Validation.RegularExpressions.ResourceGroup, ErrorMessage = Validation.Errors.Regex.ResourceGroup)]
        [StringLength(Validation.MaxLength.ResourceGroupName)]
        public string NewResourceGroupName { get; set; }

        [Required]
        [StringLength(15)]
        [ContainsAsciiOnly(ErrorMessage = Validation.Errors.Regex.NoAscii)]
        [StartsAndEndsWithNumberOrLetter(ErrorMessage = Validation.Errors.Regex.NoSpecialStartEnd)]
        [ContainsMoreThanJustNumbers(ErrorMessage = Validation.Errors.Regex.NoNumbersOnly)]
        public string VmName { get; set; }

        [Required]
        public string VmSize { get; set; }

        [Required]
        [StringLength(15)]
        public string UserName { get; set; }

        [Required]
        [StringLength(64, MinimumLength = 8)]
        public string Password { get; set; }

        [Required]
        [RegularExpression("^(/[^/ ]*)+/?$", ErrorMessage = "Invalid Linux file path")]
        public string FileShareName { get; set; }

        // NFS or SMB
        [Required]
        public string FileShareType { get; set; }

        // e.g. 10.2.0.0/24
        [RegularExpression(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\/(3[0-2]|[1-2][0-9]|[0-9]))$",
            ErrorMessage = Validation.Errors.Regex.SubnetAddressRange)]
        public string AllowedNetworks { get; set; }
    }
}
