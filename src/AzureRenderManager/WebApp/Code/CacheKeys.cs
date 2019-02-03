// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

namespace WebApp.Code
{
    public static class CacheKeys
    {
        public const string PackageList = "PackageList";
        public const string RepositoryList = "RepositoryList";
        public const string SubscriptionList = "SubscriptionList";
        public const string LocationList = "LocationList";
        public const string AccountList = "AccountList";
        public const string EnvironmentList = "EnvironmentList";
        public const string StorageAccountList = "StorageAccounts";
        public const string SubnetList = "Subnets";
        public const string AppInsightsList = "AppInsights";
        public const string PoolList = "PoolList";

        public static string MakeKey(string key, string id, params string[] extras)
        {
            var cacheKey = $"{key}-{id}";
            if (extras != null && extras.Length > 0)
            {
                cacheKey += $"-{string.Join("-", extras)}";
            }

            return cacheKey;
        }
    }
}
