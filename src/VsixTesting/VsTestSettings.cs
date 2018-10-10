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
        public string Version { get; set; } = "2012-";
        public string RootSuffix { get; set; } = "Exp";
        public int LaunchTimeoutInSeconds { get; set; } = 90;
        public bool DebugMixedMode { get; set; } = false;
        public bool ResetSettings { get; set; } = false;
        public bool SecureChannel { get; set; } = false;
        public bool AllowPreview { get; set; } = false;
        public bool PreferLowestMinorVersion { get; set; } = true;
        public string ExtensionsDirectory { get; set; } = string.Empty;
        public string ScreenshotsDirectory { get; set; } = "Screenshots";
        public bool ReuseInstance { get; set; } = true;
        public bool TakeScreenshotOnFailure { get; set; } = false;
        public bool UIThread { get; set; } = false;

        public IEnumerable<VersionRange> SupportedVersionRanges => Version.Split(';').Select(v => new VersionRange(v));
        public TimeSpan GetLaunchTimeout() => TimeSpan.FromSeconds(LaunchTimeoutInSeconds);
    }
}
