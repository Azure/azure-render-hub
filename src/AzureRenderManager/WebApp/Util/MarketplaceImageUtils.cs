using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Azure.Batch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public static SelectListItem GetSelectListItemForImage(ImageReference ir, string sku, SelectListGroup group)
        {
            var value = string.Join("|", sku, ir.Publisher, ir.Offer, ir.Sku, ir.Version);
            var name = $"{ir.Offer}: {ir.Sku} {(ir.Version == "latest" ? "" : $"({ir.Version})")}";

            foreach (var friendlyImageRef in AllowedMarketplaceImages)
            {
                if (Equal(ir, friendlyImageRef.Value))
                {
                    name = friendlyImageRef.Key;
                }
            }

            return new SelectListItem(name, value) { Group = group };
        }

        private static bool Equal(ImageReference ref1, ImageReference ref2)
        {
            return ref1.Publisher == ref2.Publisher &&
                ref1.Offer == ref2.Offer &&
                ref1.Sku == ref2.Sku;
        }
    }
}
