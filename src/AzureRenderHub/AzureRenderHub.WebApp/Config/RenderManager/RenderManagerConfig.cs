// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace WebApp.Config.RenderManager
{
    public class RenderManagerConfig
    {
        public QubeConfig Qube { get; set; }

        public DeadlineConfig Deadline { get; set; }

        public TractorConfig Tractor { get; set; }

        public OpenCueConfig OpenCue { get; set; }

        public BYOSConfig BYOS { get; set; }
    }
}
