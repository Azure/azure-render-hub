using AzureRenderHub.WebApp.Code.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using WebApp.Config.RenderManager;
using WebApp.Models.Pools;
using Xunit;

namespace AzureRenderHub.WebApp.Tests.Mapping
{
    public class LimitGroupExclusionTests
    {
        [Fact]
        public void NullExcludeFromLimitGroupsReturnsNull()
        {
            var poolConfig = new PoolConfigurationModel { ExcludeFromLimitGroups = null };
            var deadlineConfig = new DeadlineConfig();
            Assert.Null(poolConfig.GetDeadlineExcludeFromLimitGroupsString(deadlineConfig));
        }

        [Fact]
        public void EmptyExcludeFromLimitGroupsReturnsNull()
        {
            var poolConfig = new PoolConfigurationModel { ExcludeFromLimitGroups = "" };
            var deadlineConfig = new DeadlineConfig();
            Assert.Null(poolConfig.GetDeadlineExcludeFromLimitGroupsString(deadlineConfig));
        }

        [Fact]
        public void LimitGroupsAreTrimmed()
        {
            var poolConfig = new PoolConfigurationModel { ExcludeFromLimitGroups = "limitgroup,  limitgroup2" };
            var deadlineConfig = new DeadlineConfig();
            Assert.Equal("limitgroup;limitgroup2", poolConfig.GetDeadlineExcludeFromLimitGroupsString(deadlineConfig));
        }

        [Fact]
        public void PoolLimitGroupsOverrideEnvironment()
        {
            var poolConfig = new PoolConfigurationModel { ExcludeFromLimitGroups = "limitgroup" };
            var envDeadlineConfig = new DeadlineConfig { ExcludeFromLimitGroups = "environmentLimitGroup" };
            Assert.Equal("limitgroup", poolConfig.GetDeadlineExcludeFromLimitGroupsString(envDeadlineConfig));
        }

        [Fact]
        public void EnvironmentLimitGroupsIsUsedWhenNoPoolLimitGroup()
        {
            var poolConfig = new PoolConfigurationModel { ExcludeFromLimitGroups = null };
            var envDeadlineConfig = new DeadlineConfig { ExcludeFromLimitGroups = "environmentLimitGroup" };
            Assert.Equal("environmentLimitGroup", poolConfig.GetDeadlineExcludeFromLimitGroupsString(envDeadlineConfig));
        }
    }
}
