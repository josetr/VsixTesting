// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.Installer.Tests
{
    using System;
    using Vs;
    using Xunit;

    public class ProgramTests
    {
        private const string CommunityId = "Microsoft.VisualStudio.Product.Community";
        private const string ProfessionalId = "Microsoft.VisualStudio.Product.Professional";
        private const string EnterpriseId = "Microsoft.VisualStudio.Product.Enterprise";

        [Theory]
        [InlineData("2017", null, false, CommunityId)]
        [InlineData("15.0", null, false, CommunityId)]
        [InlineData("15", null, false, CommunityId)]
        [InlineData("2017", null, true, ProfessionalId)]
        [InlineData("2017", "Community", false, CommunityId)]
        [InlineData("2017", "Community", true, CommunityId)]
        [InlineData("2017", "Professional", false, ProfessionalId)]
        [InlineData("2017", "Professional", true, ProfessionalId)]
        void GetInstallation(string version, string sku, bool preview, string expected)
        {
            var installations = new[]
            {
                new VsInstallation(new Version(15, 0), string.Empty, "VisualStudio/15.0.0") { ProductId = CommunityId },
                new VsInstallation(new Version(15, 0), string.Empty, "VisualStudio/15.0.0-pre.1.0+27729.1") { ProductId = ProfessionalId },
                new VsInstallation(new Version(15, 1), string.Empty, "VisualStudio/15.0.1") { ProductId = EnterpriseId },
            };

            Assert.Equal(expected, Program.GetInstallation(installations, version, sku, preview).ProductId);
        }
    }
}
