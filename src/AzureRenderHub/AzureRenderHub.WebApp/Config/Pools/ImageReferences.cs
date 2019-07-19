// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using WebApp.Models.Pools;

namespace WebApp.Config.Pools
{
    public class ImageReferences
    {
        public List<string> SKUs { get; } = new List<string>();

        public List<PoolImageReference> CustomImages { get; } = new List<PoolImageReference>();

        public List<(string sku, PoolImageReference image)> OfficialImages { get; } = new List<(string sku, PoolImageReference image)>();
    }
}
