// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Tests
{
    using System;
    using Vs;
    using VsixTesting.XunitX.Internal;
    using Xunit;

    public class VsTestCaseFactoryTests
    {
        [Fact]
        void OneInstancePerMajorVersion()
        {
            var installations = new VsInstallation[]
            {
                new VsInstallation(new Version(15, 1), string.Empty, string.Empty),
                new VsInstallation(new Version(15, 7), string.Empty, "VisualStudio/15.7.3+27703.2026"),
            };

            Assert.Single(VsTestCaseFactory.FilterInstallations(installations, new VsTestSettings()));
        }
    }
}
