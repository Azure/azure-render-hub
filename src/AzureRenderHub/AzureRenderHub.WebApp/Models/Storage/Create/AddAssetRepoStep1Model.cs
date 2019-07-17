// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using WebApp.Config.Storage;

namespace WebApp.Models.Storage.Create
{
    public class AddAssetRepoStep1Model : AddAssetRepoBaseModel
    {
        // needs this empty constructor for model bindings
        public AddAssetRepoStep1Model()
        {  }

        public AddAssetRepoStep1Model(AssetRepository repository)
        {
            if (repository != null)
            {
                OriginalName = repository.Name;
                RepositoryName = repository.Name;
                RepositoryType = repository.RepositoryType;
                SubscriptionId = repository.SubscriptionId;
                Subnet = repository.Subnet;
                UseEnvironment = !string.IsNullOrEmpty(repository.EnvironmentName);
                SelectedEnvironmentName = repository.EnvironmentName;
            }
        }

        /// <summary>
        /// In the form, keep hold of the initially set name in case we change it.
        /// </summary>
        public string OriginalName { get; set; }
    }
}
