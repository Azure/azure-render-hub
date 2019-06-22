using Microsoft.Azure.Batch;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApp.Util
{
    public static class MarketplaceImageUtils
    {
        // Allowed images and their friendly names
        private static IReadOnlyDictionary<string, ImageReference> AllowedMarketplaceImages = new Dictionary<string, ImageReference>()
            {
                {"Azure Batch Rendering CentOS 7.3", new ImageReference("rendering-centos73", "batch", "rendering")},
                {"Azure Batch Rendering Windows 2016 Datacenter", new ImageReference("rendering-windows2016", "batch", "rendering")}
            };

        public static bool IsAllowedMarketplaceImage(ImageReference imageReference)
        {
            if (imageReference == null)
            {
                return false;
            }

            return AllowedMarketplaceImages.Values.Any(ir => Equal(ir, imageReference));
        }

        private static bool Equal(ImageReference ref1, ImageReference ref2)
        {
            return ref1.Publisher == ref2.Publisher &&
                ref1.Offer == ref2.Offer &&
                ref1.Sku == ref2.Sku;
        }
    }
}
