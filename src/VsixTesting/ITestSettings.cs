// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting
{
    internal interface ITestSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to invoke the test method in the main Visual Studio UI Thread.
        /// The default value is <see langword="false"></see>.
        /// </summary>
        bool UIThread { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to reuse the same Visual Studio Instance from a previous test.
        /// The default value is <see langword="true"></see>.
        /// </summary>
        bool ReuseInstance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to capture a screenshot if the test fails.
        /// The default value is <see langword="false"></see>.
        /// </summary>
        bool TakeScreenshotOnFailure { get; set; }
    }
}
