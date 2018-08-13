// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Tests
{
    using System;
    using System.Linq;
    using Vs;
    using VsixTesting.XunitX.Internal;
    using Xunit;

    public class VsTestCaseFactoryTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        void AllowPreview(bool allow)
        {
            var installations = new[] { new VsInstallation(new Version(15, 8), string.Empty, "VisualStudioPreview/15.8.0-pre.2.0+27729.1") };
            var testSettings = new VsTestSettings
            {
                VsAllowPreview = allow,
            };

            if (allow)
                Assert.NotEmpty(VsTestCaseFactory.FilterInstallations(installations, testSettings));
            else
                Assert.Empty(VsTestCaseFactory.FilterInstallations(installations, testSettings));
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(7, false)]
        void PreferLowest(int expectedMinorVersion, bool preferLowest)
        {
            var installations = new VsInstallation[]
            {
                new VsInstallation(new Version(15, 1), string.Empty, string.Empty),
                new VsInstallation(new Version(15, 7), string.Empty, "VisualStudio/15.7.3+27703.2026"),
            };

            var testSettings = new VsTestSettings
            {
                VsPreferLowestMinorVersion = preferLowest,
            };
            Assert.Equal(expectedMinorVersion, VsTestCaseFactory.FilterInstallations(installations, testSettings).First().Version.Minor);
        }

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
