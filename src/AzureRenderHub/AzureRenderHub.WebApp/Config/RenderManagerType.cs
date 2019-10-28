// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;

namespace WebApp.Config
{
    public enum RenderManagerType
    {
        [Description("Deadline 10")]
        Deadline = 0,

        [Description("Qube 6.10")]
        Qube610 = 1,

        [Description("Qube 7.0")]
        Qube70 = 2,

        [Description("Tractor 2")]
        Tractor2 = 3,

        [Description("OpenCue")]
        OpenCue = 4
    }
}
