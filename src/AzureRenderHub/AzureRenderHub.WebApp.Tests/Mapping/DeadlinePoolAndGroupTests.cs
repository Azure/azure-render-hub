using AzureRenderHub.WebApp.Code.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using WebApp.Models.Pools;
using Xunit;

namespace AzureRenderHub.WebApp.Tests.Mapping
{
    public class DeadlinePoolAndGroupTests
    {
        [Fact]
        public void DefaultsToPoolName()
        {
            var poolConfig = new PoolConfigurationModel { PoolName = "mypool" };
            Assert.Equal("mypool", poolConfig.GetDeadlinePoolsString());
            Assert.Null(poolConfig.GetDeadlineGroupsString());
        }

        [Fact]
        public void UseGroupInsteadOfPool()
        {
            var poolConfig = new PoolConfigurationModel { PoolName = "mypool", UseDeadlineGroups = true, };
            Assert.Equal("mypool", poolConfig.GetDeadlineGroupsString());
            Assert.Null(poolConfig.GetDeadlinePoolsString());
        }

        [Fact]
        public void CanSpecifyAdditionalPools()
        {
            var poolConfig = new PoolConfigurationModel { PoolName = "mypool", AdditionalPools = "pool1,pool2" };
            Assert.Equal("mypool,pool1,pool2", poolConfig.GetDeadlinePoolsString());
            Assert.Null(poolConfig.GetDeadlineGroupsString());
        }

        [Fact]
        public void CanSpecifyAdditionalGroups()
        {
            var poolConfig = new PoolConfigurationModel { PoolName = "mypool", AdditionalGroups = "group1,group2" };
            Assert.Equal("group1,group2", poolConfig.GetDeadlineGroupsString());
        }

        [Fact]
        public void CanUseGroupsInsteadOfPoolsAndSpecifyAdditionalGroups()
        {
            var poolConfig = new PoolConfigurationModel { PoolName = "mypool", UseDeadlineGroups = true, AdditionalGroups = "group1,group2" };
            Assert.Equal("mypool,group1,group2", poolConfig.GetDeadlineGroupsString());
        }
    }
}
