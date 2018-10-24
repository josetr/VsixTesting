// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting
{
    internal interface ITestSettings
    {
        /// <summary>
        /// Gets or sets the version range.
        /// The default value is 2012- which targets Visual Studio 2012 and higher.
        /// </summary>
        /// <remarks>
        /// The full version format used in the vsixmanifest is supported.
        /// </remarks>
        string Version { get; set; }

        /// <summary>
        /// Gets or sets the Visual Studio Root Suffix.
        /// The default value is `Exp` which targets the default Visual Studio Experimental Instance.
        /// See <see href="https://docs.microsoft.com/en-us/visualstudio/extensibility/the-experimental-instance" /> for more information.
        /// </summary>
        string RootSuffix { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the mixed 'Managed/Native' debugging engine should be used to attach/debug the remote Visual Studio Instance.
        /// The default is <see langword="false" />.
        /// </summary>
        bool DebugMixedMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the channel used to communicate to the remote Visual Studio Instance must be secure.
        /// The default value is <see langword="false" /> because if it's set to <see langword="true" /> then "xunit.AppDomain" must be set to <see langword="denied" /> as well.
        /// </summary>
        bool SecureChannel { get; set; }

        /// <summary>
        /// Gets or sets a directory path containing .vsix packages to install before launching the Visual Studio Instance.
        /// </summary>
        /// <remarks>
        /// The path is relative to the tested assembly, unless an absolute path is given.
        /// </remarks>
        string ExtensionsDirectory { get; set; }

        /// <summary>
        /// Gets or sets a directory path where screenshots will be stored when an error occurs within the Visual Studio Instance.
        /// </summary>
        string ScreenshotsDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to invoke the test method in the main Visual Studio UI Thread.
        /// The default value is <see langword="false" />.
        /// </summary>
        bool UIThread { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to reuse the same Visual Studio Instance from a previous test.
        /// The default value is <see langword="true" />.
        /// </summary>
        bool ReuseInstance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to capture a screenshot if the test fails.
        /// The default value is <see langword="false" />.
        /// </summary>
        bool TakeScreenshotOnFailure { get; set; }
    }
}
