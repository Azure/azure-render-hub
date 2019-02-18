// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WebApp.Models.Reporting
{
    public class EnvironmentUsage
    {
        public EnvironmentUsage(string envId, Usage usage)
        {
            EnvironmentId = envId;
            Usage = usage;
        }

        public string EnvironmentId { get; }

        public Usage Usage { get; }
    }

}
