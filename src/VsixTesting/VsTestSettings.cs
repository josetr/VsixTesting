// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Vs;

    internal class VsTestSettings : ITestSettings
    {
        internal static readonly VsTestSettings Defaults = new VsTestSettings();
        public string VsVersion { get; set; } = "2012-";
        public string VsRootSuffix { get; set; } = "Exp";
        public int VsLaunchTimeoutInSeconds { get; set; } = 45;
        public bool VsDebugMixedMode { get; set; } = false;
        public bool VsResetSettings { get; set; } = false;
        public bool VsSecureChannel { get; set; } = false;
        public bool VsAllowPreview { get; set; } = false;
        public bool VsPreferLowestMinorVersion { get; set; } = true;
        public string VsExtensionsDirectory { get; set; } = string.Empty;
        public string ScreenshotsDirectory { get; set; } = "Screenshots";
        public bool ReuseInstance { get; set; } = true;
        public bool TakeScreenshotOnFailure { get; set; } = false;
        public bool UIThread { get; set; } = false;

        public IEnumerable<VersionRange> VsSupportedVersionRanges => VsVersion.Split(';').Select(v => new VersionRange(v));
        public string[] GetExtensionsToInstall() => Directory.GetFiles(VsExtensionsDirectory, "*.vsix");
        public TimeSpan GetLaunchTimeout() => TimeSpan.FromSeconds(VsLaunchTimeoutInSeconds);
    }
}