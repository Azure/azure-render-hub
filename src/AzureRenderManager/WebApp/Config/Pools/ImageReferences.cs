// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using Microsoft.Azure.Batch;

namespace WebApp.Config.Pools
{
    public class ImageReferences
    {
        public List<string> SKUs { get; } = new List<string>();

        public List<ImageReference> CustomImages { get; } = new List<ImageReference>();

        public List<(string sku, ImageReference image)> OfficialImages { get; } = new List<(string sku, ImageReference image)>();
    }
}
