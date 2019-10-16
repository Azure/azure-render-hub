using Microsoft.Azure.Management.Batch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Code;
using WebApp.Code.Extensions;
using WebApp.Config.RenderManager;
using WebApp.Models.Pools;

namespace AzureRenderHub.WebApp.Code.Extensions
{
    public static class PoolConfigurationExtensions
    {
        public static string GetDeadlinePoolsString(this PoolConfigurationModel poolConfiguration)
        {
            if (poolConfiguration == null)
            {
                return null;
            }

            var pools = poolConfiguration.UseDeadlineGroups ? "" : poolConfiguration.PoolName;
            if (poolConfiguration.AdditionalPools != null && poolConfiguration.AdditionalPools.Any())
            {
                pools += string.IsNullOrEmpty(pools) ? "" : ",";
                pools += string.Join(',', poolConfiguration.AdditionalPools);
            }

            return string.IsNullOrEmpty(pools) ? null : pools;
        }

        public static string GetDeadlineGroupsString(this PoolConfigurationModel poolConfiguration)
        {
            if (poolConfiguration == null)
            {
                return null;
            }

            var groups = poolConfiguration.UseDeadlineGroups ? poolConfiguration.PoolName : "";
            if (poolConfiguration.AdditionalGroups != null && poolConfiguration.AdditionalGroups.Any())
            {
                groups += string.IsNullOrEmpty(groups) ? "" : ",";
                groups += string.Join(',', poolConfiguration.AdditionalGroups);
            }

            return string.IsNullOrEmpty(groups) ? null : groups;
        }

        public static string GetDeadlineExcludeFromLimitGroupsString(this PoolConfigurationModel poolConfiguration, DeadlineConfig deadlineConfig)
        {
            if (poolConfiguration == null)
            {
                return null;
            }

            // Get pool limit groups, or environment limitgroups, if specified.
            var limitgroups = !string.IsNullOrWhiteSpace(poolConfiguration.ExcludeFromLimitGroups)
                ? poolConfiguration.ExcludeFromLimitGroups
                : deadlineConfig != null && !string.IsNullOrWhiteSpace(deadlineConfig.ExcludeFromLimitGroups) ? deadlineConfig.ExcludeFromLimitGroups : null;

            if (!string.IsNullOrWhiteSpace(limitgroups))
            {
                var trimmed = limitgroups
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(lg => lg.Trim());

                if (trimmed.Any())
                {
                    // Multiple limit groups are separated by a space
                    return string.Join(';', trimmed);
                }
            }

            return null;
        }

        public static void AddDeadlinePoolsAndGroups(this IList<MetadataItem> metadata, PoolConfigurationModel poolConfiguration)
        {
            if (metadata == null)
            {
                return;
            }

            var pools = poolConfiguration.GetDeadlinePoolsString();
            if (!string.IsNullOrEmpty(pools))
            {
                metadata.AddOrUpdateMetadata(MetadataKeys.DeadlinePools, pools);
            }

            var groups = poolConfiguration.GetDeadlineGroupsString();
            if (!string.IsNullOrEmpty(groups))
            {
                metadata.AddOrUpdateMetadata(MetadataKeys.DeadlineGroups, groups);
            }
        }
    }
}
