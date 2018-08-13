// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Vs.Tests
{
    using System;
    using Xunit;

    public class VersionTests
    {
        [Theory]
        [InlineData(2012, 11)]
        [InlineData(2013, 12)]
        [InlineData(2015, 14)]
        [InlineData(2017, 15)]
        void GetYearFromMajorVersion(int expectedYear, int majorVersion)
            => Assert.Equal(expectedYear, VersionUtil.GetYear(new Version(majorVersion, 0)));

        [Theory]
        [InlineData(11, 2012)]
        [InlineData(12, 2013)]
        [InlineData(14, 2015)]
        [InlineData(15, 2017)]
        void GetMajorVersionFromYear(int expectedMajorVersion, int year)
            => Assert.Equal(VersionUtil.FromYear(year).Major, expectedMajorVersion);

        [Theory]
        [InlineData(10)] // Not supported
        [InlineData(13)] // Doesn't exist
        [InlineData(16)] // Not supported yet
        void BadMajorVersions(int major)
            => Assert.Throws<ArgumentOutOfRangeException>(() => VersionUtil.GetYear(new Version(major, 0)));

        [Theory]
        [InlineData(2010)] // Not supported
        [InlineData(2014)] // Doesn't exist
        [InlineData(2016)] // Doesn't exist
        [InlineData(2018)] // Doesn't exist
        [InlineData(2019)] // Not supported yet
        void BadYears(int year)
            => Assert.Throws<ArgumentOutOfRangeException>(() => VersionUtil.FromYear(year));
    }
}
