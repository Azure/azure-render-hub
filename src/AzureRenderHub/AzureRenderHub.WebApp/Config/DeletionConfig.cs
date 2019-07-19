// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WebApp.Config
{
    public class DeletionSettings
    {
        public bool DeleteResourceGroup { get; set; }

        public bool DeleteBatchAccount { get; set; }

        public bool DeleteStorageAccount { get; set; }

        public bool DeleteAppInsights { get; set; }

        public bool DeleteKeyVault { get; set; }

        public bool DeleteVNet { get; set; }

        public string DeleteErrors { get; set; }
    }
}
