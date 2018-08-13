// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Xunit
{
    using System;
    using VsixTesting;

    /// <summary>
    /// Attribute used to configure the Visual Studio Instance where test methods decorated with VsFact / VsTheory will run.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public class VsInstanceAttribute : Attribute, IInstanceSettings
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
    }
}
