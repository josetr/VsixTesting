﻿// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Xunit
{
    using System;
    using VsixTesting;

    /// <summary>
    /// Attribute used to specify the test settings for test methods decorated with VsFact or VsTheory.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
    public class VsTestSettingsAttribute : Attribute, ITestSettings
    {
        /// <inheritdoc />
        public string Version { get; set; }

        /// <inheritdoc />
        public string RootSuffix { get; set; }

        /// <inheritdoc />
        public bool DebugMixedMode { get; set; }

        /// <inheritdoc />
        public bool SecureChannel { get; set; }

        /// <inheritdoc />
        public bool AllowPreview { get; set; }

        /// <inheritdoc />
        public string ExtensionsDirectory { get; set; }

        /// <inheritdoc />
        public string ScreenshotsDirectory { get; set; }

        /// <inheritdoc />
        public bool UIThread { get; set; }

        /// <inheritdoc />
        public bool ReuseInstance { get; set; }

        /// <inheritdoc />
        public bool TakeScreenshotOnFailure { get; set; }
    }
}