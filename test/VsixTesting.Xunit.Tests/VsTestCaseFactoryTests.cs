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

        [Theory]
        [InlineData("14", "VisualStudio/14.0.0")]
        [InlineData("14-", "VisualStudio/14.0.0")]
        [InlineData("15", "VisualStudio/15.0.2")]
        void FilterInstallations_PreferSpecificInstallation(string version, string expectedName)
        {
            var installationPath = "C:\\Program Files (x86)\\Microsoft Visual Studio 15.0";
            var installations = new[]
            {
                new VsInstallation(new Version(14, 0, 0), string.Empty, "VisualStudio/14.0.0"),
                new VsInstallation(new Version(15, 0, 3), string.Empty, "VisualStudio/15.0.3"),
                new VsInstallation(new Version(15, 0, 2), installationPath, "VisualStudio/15.0.2"),
                new VsInstallation(new Version(15, 0, 1), string.Empty, "VisualStudio/15.0.1"),
            };

            var applicationPath = VisualStudioUtil.GetApplicationPath(installationPath);
            var installation = VsTestCaseFactory.FilterInstallations(installations, new VsTestSettings { Version = version }, preferedAppPath: applicationPath).First();
            Assert.Equal(expectedName, installation.Name);
        }
    }
}
