// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Management.Batch.Models;
using WebApp.Config;
using WebApp.Models.Pools;
using MetadataItem = Microsoft.Azure.Management.Batch.Models.MetadataItem;

namespace WebApp.Code.Extensions
{
    public static class AutoScaleMetadataExtensions
    {
        public static void AddAutoScaleMetadata(this IList<MetadataItem> metadata, PoolBaseModel model)
        {
            if (metadata == null)
            {
                return;
            }

            AddOrUpdateMetadata(metadata, MetadataKeys.AutoScaleDownPolicy, model.AutoScalePolicy.ToString());
            AddOrUpdateMetadata(metadata, MetadataKeys.AutoScaleDownTimeout, model.AutoScaleDownIdleTimeout.ToString());
            AddOrUpdateMetadata(metadata, MetadataKeys.AutoScaleMinimumDedicatedNodes, model.MinimumDedicatedNodes.ToString());
            AddOrUpdateMetadata(metadata, MetadataKeys.AutoScaleMinimumLowPriorityNodes, model.MinimumLowPriorityNodes.ToString());
            AddOrUpdateMetadata(metadata, MetadataKeys.AutoScaleMaximumDedicatedNodes, model.MaximumDedicatedNodes.ToString());
            AddOrUpdateMetadata(metadata, MetadataKeys.AutoScaleMaximumLowPriorityNodes, model.MaximumLowPriorityNodes.ToString());
        }

        public static bool GetAutoScaleEnabled(this CloudPool pool)
        {
            return GetAutoScaleEnabled(pool.Metadata.ToDictionary());
        }

        public static bool GetAutoScaleEnabled(this Pool pool)
        {
            return GetAutoScaleEnabled(pool.Metadata.ToDictionary());
        }

        private static bool GetAutoScaleEnabled(Dictionary<string, string> metadata)
        {
            return metadata.ContainsKey(MetadataKeys.AutoScaleDownEnabled) &&
                   metadata[MetadataKeys.AutoScaleDownEnabled] == bool.TrueString;
        }

        public static int GetAutoScaleTimeoutInMinutes(this CloudPool pool)
        {
            return GetAutoScaleTimeoutInMinutes(pool.Metadata.ToDictionary());
        }

        public static int GetAutoScaleTimeoutInMinutes(this Pool pool)
        {
            return GetAutoScaleTimeoutInMinutes(pool.Metadata.ToDictionary());
        }

        private static int GetAutoScaleTimeoutInMinutes(Dictionary<string, string> metadata)
        {
            return GetIntFromMetadata(metadata, MetadataKeys.AutoScaleDownTimeout, 30);
        }

        public static int GetAutoScaleMaximumDedicatedNodes(this CloudPool pool)
        {
            return GetAutoScaleMaximumDedicatedNodes(pool.Metadata.ToDictionary());
        }

        public static int GetAutoScaleMaximumDedicatedNodes(this Pool pool)
        {
            return GetAutoScaleMaximumDedicatedNodes(pool.Metadata.ToDictionary());
        }

        private static int GetAutoScaleMaximumDedicatedNodes(Dictionary<string, string> metadata)
        {
            return GetIntFromMetadata(metadata, MetadataKeys.AutoScaleMaximumDedicatedNodes, 30);
        }

        public static int GetAutoScaleMaximumLowPriorityNodes(this CloudPool pool)
        {
            return GetAutoScaleMaximumLowPriorityNodes(pool.Metadata.ToDictionary());
        }

        public static int GetAutoScaleMaximumLowPriorityNodes(this Pool pool)
        {
            return GetAutoScaleMaximumLowPriorityNodes(pool.Metadata.ToDictionary());
        }

        private static int GetAutoScaleMaximumLowPriorityNodes(Dictionary<string, string> metadata)
        {
            return GetIntFromMetadata(metadata, MetadataKeys.AutoScaleMaximumLowPriorityNodes, 30);
        }

        public static int GetAutoScaleMinimumDedicatedNodes(this CloudPool pool)
        {
            return GetAutoScaleMinimumDedicatedNodes(pool.Metadata.ToDictionary());
        }

        public static int GetAutoScaleMinimumDedicatedNodes(this Pool pool)
        {
            return GetAutoScaleMinimumDedicatedNodes(pool.Metadata.ToDictionary());
        }

        private static int GetAutoScaleMinimumDedicatedNodes(Dictionary<string, string> metadata)
        {
            return GetIntFromMetadata(metadata, MetadataKeys.AutoScaleMinimumDedicatedNodes, 30);
        }

        public static int GetAutoScaleMinimumLowPriorityNodes(this CloudPool pool)
        {
            return GetAutoScaleMinimumLowPriorityNodes(pool.Metadata.ToDictionary());
        }

        public static int GetAutoScaleMinimumLowPriorityNodes(this Pool pool)
        {
            return GetAutoScaleMinimumLowPriorityNodes(pool.Metadata.ToDictionary());
        }

        private static int GetAutoScaleMinimumLowPriorityNodes(Dictionary<string, string> metadata)
        {
            return GetIntFromMetadata(metadata, MetadataKeys.AutoScaleMinimumLowPriorityNodes, 30);
        }

        public static AutoScalePolicy GetAutoScalePolicy(this CloudPool pool)
        {
            return GetAutoScalePolicy(pool.Metadata.ToDictionary());
        }

        public static AutoScalePolicy GetAutoScalePolicy(this Pool pool)
        {
            return GetAutoScalePolicy(pool.Metadata.ToDictionary());
        }

        private static AutoScalePolicy GetAutoScalePolicy(Dictionary<string, string> metadata)
        {
            return GetEnumFromMetadata<AutoScalePolicy>(metadata, MetadataKeys.AutoScaleDownPolicy);
        }

        private static Dictionary<string, string> ToDictionary(this IList<MetadataItem> metadata)
        {
            if (metadata == null)
            {
                return new Dictionary<string, string>();
            }
            return metadata.ToDictionary(x => x.Name, y => y.Value);
        }

        private static Dictionary<string, string> ToDictionary(this IList<Microsoft.Azure.Batch.MetadataItem> metadata)
        {
            if (metadata == null)
            {
                return new Dictionary<string, string>();
            }
            return metadata.ToDictionary(x => x.Name, y => y.Value);
        }

        private static int GetIntFromMetadata(Dictionary<string, string> metadata, string key, int defaultValue)
        {
            if (metadata.TryGetValue(key, out var min))
            {
                if (int.TryParse(min, out var minimum))
                {
                    return minimum;
                }
            }
            return defaultValue;
        }

        private static TEnum GetEnumFromMetadata<TEnum>(Dictionary<string, string> metadata, string key) where TEnum : struct
        {
            TEnum result = default(TEnum);
            if (metadata.TryGetValue(key, out string v))
            {
                if (Enum.TryParse(v, true, out result))
                {
                    return result;
                }
            }
            return result;
        }

        private static void AddOrUpdateMetadata(IList<MetadataItem> metadata, string name, string value)
        {
            var item = metadata.FirstOrDefault(m => m.Name == name);
            if (item != null)
            {
                item.Value = value;
            }
            else
            {
                metadata.Add(new MetadataItem(name, value));
            }
        }
    }
}
