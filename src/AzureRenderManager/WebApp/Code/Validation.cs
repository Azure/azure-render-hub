// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WebApp.Code
{
    public static class Validation
    {
        public static class Errors
        {
            public static class Regex
            {
                public const string PackageName = "Package names only allow alphanumeric characters, periods, underscores, and hyphens and cannot end in a period.";
                public const string AssetRepoName = "Storage configuration names only allow alphanumeric characters, periods, underscores, and hyphens and cannot end in a period.";
                public const string ResourceGroup = "Resource group names only allow alphanumeric characters, periods, underscores, and hyphens and cannot end in a period.";
                public const string SubnetAddressRange = "Subnet address ranges must be a valid CIDR block such as 10.1.0.0/24.";
                public const string NoAscii = "{0} cannot contain non-ASCII or special characters.";
                public const string NoSpecialStartEnd = "{0} must begin and end with a letter or number.";
                public const string NoNumbersOnly = "{0} cannot contain only numbers.";
                public const string EnvironmentName = "Environment name can only contain any combination of alphanumeric characters including hyphens and underscores.";
                public const string PoolName = "Pool name can only contain any combination of alphanumeric characters including hyphens and underscores.";
            }

            public static class Required
            {
                public const string ResourceGroup = "The resource group name cannot be empty and must not exist in the Azure subscription.";
            }

            public static class Custom
            {
                public const string ImageRefOrCustom = "An official image reference or a custom image must be selected.";
                public const string CustomAndSku = "For a custom image, both an image reference and a Batch agent SKU must be selected.";
                public const string FormSummary = "You have validation errors in the form below. Please fix and re-submit the form.";
                public const string PackageNotFound = "Selected package does not exist.";
            }
        }

        public static class RegularExpressions
        {
            public const string AsciiOnly = "^[a-zA-Z0-9-]+$";
            public const string NumbersOnly = "^[\\d]+$";
            public const string KeyVault = "^[a-zA-Z][A-Za-z0-9]*(?:-[A-Za-z0-9]+)*$";
            public const string CommaSeparatedList = "^([a-zA-Z0-9]+,?\\s*)*$";

            public const string EnvironmentName = "^[_\\w\\-]+$";
            public const string PoolName = EnvironmentName;

            public const string ResourceGroup = "^[-\\w\\._]+$";
            public const string AssetRepoName = ResourceGroup;
            public const string PackageName = ResourceGroup;
            public const string VNetName = ResourceGroup;
            public const string AppInsightsName = ResourceGroup;

            public const string BatchAccountName = "^[a-z0-9]+$"; // lowercase letters and numbers only
            public const string StorageAccountName = BatchAccountName;

            public const string FileShareName = "^[a-z0-9]+(-[a-z0-9]+)*$";
        }

        public class MaxLength
        {
            public const int ResourceGroupName = 80;
        }
    }
}
