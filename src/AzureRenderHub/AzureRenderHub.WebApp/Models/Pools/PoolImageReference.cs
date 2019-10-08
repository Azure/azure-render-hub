using Microsoft.Azure.Management.Compute.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Models.Pools
{
    public class PoolImageReference
    {
        private static IReadOnlyDictionary<string, PoolImageReference> AllowedMarketplaceImages = new Dictionary<string, PoolImageReference>()
            {
                {"Azure Batch Rendering CentOS 7.3", new PoolImageReference("batch", "rendering-centos73", "rendering")},
                {"Azure Batch Rendering Windows 2016 Datacenter", new PoolImageReference("batch", "rendering-windows2016", "rendering")}
            };

        private static string Delimiter = "|";

        // From Batch Image Ref
        public PoolImageReference(Microsoft.Azure.Batch.ImageReference imageReference, string os)
        {
            Publisher = imageReference.Publisher;
            Offer = imageReference.Offer;
            Sku = imageReference.Sku;
            Version = imageReference.Version;
            Type = ImageReferenceType.Marketplace;
            Os = ParseOperatingSystem(os);
        }

        // From Compute Image
        public PoolImageReference(Image image)
        {
            CustomImageResourceId = image.Id;
            Type = ImageReferenceType.Custom;
            Os = ParseOperatingSystem(image.StorageProfile.OsDisk.OsType.ToString());
        }

        public PoolImageReference(GalleryImage galleryImage, GalleryImageVersion galleryImageVersion)
        {
            CustomImageResourceId = galleryImageVersion.Id;
            Type = ImageReferenceType.Gallery;
            Os = ParseOperatingSystem(galleryImage.OsType.ToString());
        }

        public PoolImageReference(string publisher, string offer, string sku, string version = null)
        {
            Publisher = publisher;
            Offer = offer;
            Sku = sku;
            Version = version;
            Type = ImageReferenceType.Marketplace;
        }

        // From value
        public PoolImageReference(string concatenatedValue)
        {
            if (string.IsNullOrWhiteSpace(concatenatedValue))
            {
                throw new ArgumentNullException("concatenatedValue");
            }

            var tokens = concatenatedValue.Split(Delimiter);

            if (tokens.Length == 4)
            {
                Type = Enum.Parse<ImageReferenceType>(tokens[0]);
                CustomImageResourceId = tokens[1];
                // name = tokens[2]
                Os = ParseOperatingSystem(tokens[3]);
            }
            else if (tokens.Length == 6)
            {
                // tokens[0] is the node agent SKU
                Publisher = tokens[1];
                Offer = tokens[2];
                Sku = tokens[3];
                Version = tokens[4];
                Os = ParseOperatingSystem(tokens[5]);
                Type = ImageReferenceType.Marketplace;
            }
            else
            {
                throw new ArgumentException($"Invalid value specified {concatenatedValue}");
            }
        }

        private OperatingSystem ParseOperatingSystem(string os)
        {
            OperatingSystem parsedOs;
            if (!Enum.TryParse<OperatingSystem>(os, out parsedOs))
            {
                throw new ArgumentException($"Invalid operating system specified {os}");
            }
            return parsedOs;
        }

        public ImageReferenceType Type { get; }

        // Custom Image
        public string CustomImageResourceId { get; }

        // or

        // Marketplace image
        public string Publisher { get; }

        public string Offer { get; }

        public string Sku { get; }

        public string NodeAgentSku
        {
            get
            {
                if (Os == OperatingSystem.Windows)
                {
                    return "batch.node.windows amd64";
                }

                // We only support CentOS at the moment
                return "batch.node.centos 7";
            }
        }

        public string Version { get; }

        // Friedly Name
        public string Name
        {
            get
            {
                if (Type == ImageReferenceType.Custom)
                {
                    return CustomImageResourceId?.Substring(CustomImageResourceId.LastIndexOf('/') + 1);
                }

                if (Type == ImageReferenceType.Gallery)
                {
                    var tokens = CustomImageResourceId?.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var imageName = tokens?.Length >= 10 ? tokens[9] : "Unknown";
                    var imageVersion = tokens?.LastOrDefault();
                    return $"{imageName} ({imageVersion})";
                }

                var name = $"{Offer}: {Sku} {(Version == "latest" ? "" : $"({Version})")}";

                foreach (var friendlyNameToImageRef in AllowedMarketplaceImages)
                {
                    if (SkusEqual(friendlyNameToImageRef.Value))
                    {
                        // We have a friendly name, probably for the rendering image
                        name = friendlyNameToImageRef.Key;
                        break;
                    }
                }

                return name;
            }
        }

        // Unique identifier for this reference, this can also be used to instantiate an instance
        // to support posting from a form
        public string Value
        {
            get
            {
                if (Type == ImageReferenceType.Custom || Type == ImageReferenceType.Gallery)
                {
                    return $"{Type}{Delimiter}{CustomImageResourceId}{Delimiter}{Name}{Delimiter}{Os}";
                }
                return string.Join(Delimiter, NodeAgentSku, Publisher, Offer, Sku, Version, Os);
            }
        }

        public OperatingSystem Os { get; set; }

        public bool SkusEqual(PoolImageReference other)
        {
            return Publisher == other.Publisher &&
                Offer == other.Offer &&
                Sku == other.Sku;
        }
    }

    public enum ImageReferenceType
    {
        Marketplace,
        Custom,
        Gallery,
    }

    public enum OperatingSystem
    {
        Windows,
        Linux
    }
}
