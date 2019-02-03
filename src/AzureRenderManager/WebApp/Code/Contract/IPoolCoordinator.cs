// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Azure.Management.Batch.Models;

using WebApp.Config;
using WebApp.Config.Pools;

namespace WebApp.Code.Contract
{
    /// <summary>
    /// Changed to IPoolCoordinator as IPoolOperation is already used and gets confusing
    /// </summary>
    public interface IPoolCoordinator
    {
        Task CreatePool(RenderingEnvironment environment, Pool pool);

        Task UpdatePool(RenderingEnvironment environment, Pool pool);

        Task DeletePool(RenderingEnvironment environment, string poolName);

        Task<Pool> GetPool(RenderingEnvironment environment, string poolName);

        void ClearCache(string envId);

        Task<List<Pool>> ListPools(RenderingEnvironment environment);

        Task<ImageReferences> GetImageReferences(RenderingEnvironment environment);
    }
}