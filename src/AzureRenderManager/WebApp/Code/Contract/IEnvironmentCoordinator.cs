// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApp.Config;

namespace WebApp.Code.Contract
{
    public interface IEnvironmentCoordinator
    {
        /// <summary>
        /// Get an environment based on the name
        /// </summary>
        /// <param name="envId">ID or Name of the environment</param>
        /// <returns></returns>
        Task<RenderingEnvironment> GetEnvironment(string envId);

        /// <summary>
        /// Remove the specified environment from the config store
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        Task<bool> RemoveEnvironment(RenderingEnvironment environment);

        /// <summary>
        /// Update the specified environment
        /// </summary>
        /// <param name="environment">environment to update</param>
        /// <param name="originalName">if we have changed the name we will pass in the original name</param>
        /// <returns></returns>
        Task UpdateEnvironment(RenderingEnvironment environment, string originalName = null);

        /// <summary>
        /// List the environments from the config
        /// </summary>
        /// <returns></returns>
        Task<List<string>> ListEnvironments();
    }
}
