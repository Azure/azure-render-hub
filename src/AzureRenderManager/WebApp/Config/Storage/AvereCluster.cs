// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using WebApp.Models.Storage.Create;

namespace WebApp.Config.Storage
{
    public class AvereCluster : AssetRepository
    {
        public AvereCluster()
        {
            RepositoryType = AssetRepositoryType.AvereCluster;
        }

        public override void UpdateFromModel(AddAssetRepoBaseModel avereModel)
        {

        }
    }
}
