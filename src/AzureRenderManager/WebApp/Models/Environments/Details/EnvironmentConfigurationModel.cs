// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using WebApp.Config;
using WebApp.Config.Pools;

namespace WebApp.Models.Environments.Details
{
    public class EnvironmentConfigurationModel : EnvironmentBaseModel
    {
        public EnvironmentConfigurationModel()
        {
        }

        public EnvironmentConfigurationModel(RenderingEnvironment environment, StartTaskProvider startTaskProvider, string environmentEndpoint = null)
        {
            if (environment != null)
            {
                EnvironmentName = environment.Name;
                MaxIdleCpuPercent = 15; // Defaults
                MaxIdleGpuPercent = 2;

                if (environment.AutoScaleConfiguration != null)
                {
                    MaxIdleCpuPercent = environment.AutoScaleConfiguration.MaxIdleCpuPercent;
                    MaxIdleGpuPercent = environment.AutoScaleConfiguration.MaxIdleGpuPercent;

                    if (environment.AutoScaleConfiguration.SpecificProcesses != null)
                    {
                        SpecificProcesses = string.Join(',', environment.AutoScaleConfiguration.SpecificProcesses);
                    }

                    ScaleEndpointEnabled = environment.AutoScaleConfiguration.ScaleEndpointEnabled;
                    PrimaryApiKey = environment.AutoScaleConfiguration.PrimaryApiKey;
                    SecondaryApiKey = environment.AutoScaleConfiguration.SecondaryApiKey;
                }
            }

            WindowsBootstrapScript = startTaskProvider.GetWindowsStartTaskUrl(environment);
            LinuxBootstrapScript = startTaskProvider.GetLinuxStartTaskUrl(environment);
            EnvironmentEndpoint = environmentEndpoint;
        }

        [Required]
        [Range(1, 100)]
        [Display(Name = "Max CPU idle percentage")]
        public int MaxIdleCpuPercent { get; set; }

        [Required]
        [Range(1, 100)]
        [Display(Name = "Max GPU idle percentage")]
        public int MaxIdleGpuPercent { get; set; }

        [Display(Name = "Specific processes")]
        public string SpecificProcesses { get; set; }

        public bool ScaleEndpointEnabled { get; set; }

        [Display(Name = "API Endpoint")]
        public string EnvironmentEndpoint { get; set; }

        // The secure authentication key required for access
        public string PrimaryApiKey { get; set; }

        public string SecondaryApiKey { get; set; }

        public string WindowsBootstrapScript { get; set; }

        public string LinuxBootstrapScript { get; set; }
    }
}
