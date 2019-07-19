// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WebApp.Code
{
    public static class MetadataKeys
    {
        public const string Package = "RenderManagerPackage";
        public const string GpuPackage = "GPUPackage";
        public const string GeneralPackages = "GeneralPackages";
        public const string AutoScaleDownEnabled = "AutoScaleDownEnabled";
        public const string AutoScaleDownTimeout = "AutoScaleDownTimeout";
        public const string AutoScaleDownPolicy = "AutoScaleDownPolicy";
        public const string AutoScaleMinimumDedicatedNodes = "MinimumDedicatedNodes";
        public const string AutoScaleMinimumLowPriorityNodes = "MinimumLowPriorityNodes";
        public const string AutoScaleMaximumDedicatedNodes = "MaximumDedicatedNodes";
        public const string AutoScaleMaximumLowPriorityNodes = "MaximumLowPriorityNodes";
        public const string UseDeadlineGroups = "UseDeadlineGroups";
    }
}
