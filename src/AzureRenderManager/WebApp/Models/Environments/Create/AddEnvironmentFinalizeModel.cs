// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using WebApp.Config;

namespace WebApp.Models.Environments.Create
{
    public class AddEnvironmentFinalizeModel : EnvironmentBaseModel
    {
        // needs this empty constructor for model bindings
        public AddEnvironmentFinalizeModel() { }

        public AddEnvironmentFinalizeModel(RenderingEnvironment environment)
        {
            if (environment != null)
            {
                EnvironmentName = environment.Name;
            }
        }
    }
}
