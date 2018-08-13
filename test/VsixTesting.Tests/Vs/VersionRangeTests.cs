// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Vs.Tests
{
    using System;
    using Xunit;

    public class VersionRangeTests
    {
        [Theory]
        [InlineData("11.0", "11.0")]
        [InlineData("12.0", "12.0")]
        [InlineData("13.0", "13.0")]
        [InlineData("14.0", "14.0")]
        [InlineData("15.0", "15.0")]
        [InlineData("15.5.6", "15.5.6")]
        [InlineData("15.5.6.7", "15.5.6.7")]
        void ParseSingleVersion(string expected, string input)
        {
            var range = VersionRange.Parse(input);
            Assert.Equal(Version.Parse(expected), range.Minimum);
            Assert.Equal(Version.Parse(expected), range.Maximum);
        }

        [Theory]
        [InlineData("11.0", "16.0", "[11.0-16.0]")]
        [InlineData("11.0", "15.2147483647", "[11.0-16.0)")]
        [InlineData("11.1", "16.0", "(11.0-16.0]")]
        [InlineData("11.0", "15.2147483647", "11-15")]
        [InlineData("11.0", "11.2147483647", "11")]
        [InlineData("15.0", "15.2147483647", "15")]
        [InlineData("11.1.2.7", "12.5.5.7", "[11.1.2.7-12.5.5.7]")]
        void ParseRange(string minVersionExpected, string maxVersionExpected, string input)
        {
            var range = VersionRange.Parse(input);
            Assert.Equal(Version.Parse(minVersionExpected), range.Minimum);
            Assert.Equal(Version.Parse(maxVersionExpected), range.Maximum);
        }

        [Theory]
        [InlineData("11.0", "11.0-")]
        [InlineData("11.0", "[11.0-")]
        [InlineData("11.0", "11.0-]")]
        [InlineData("11.0", "[11.0-]")]
        [InlineData("11.1", "(11.0-]")]
        void ParseUnboundedMaxVersion(string expected, string input)
        {
            var range = VersionRange.Parse(input);
            Assert.Equal(Version.Parse(expected), range.Minimum);
            Assert.Equal(new Version(int.MaxValue, int.MaxValue), range.Maximum);
        }

        [Theory]
        [InlineData("(11.0")]
        [InlineData("[11.0")]
        [InlineData("11.0]")]
        [InlineData("11.0)")]
        [InlineData("[-]")]
        void InvalidFormatThrows(string input)
            => Assert.Throws<NotSupportedException>(() => VersionRange.Parse(input));

        [Fact]
        void EmptyFormatThrows()
            => Assert.Throws<ArgumentException>(() => VersionRange.Parse(string.Empty));

        [Theory]
        [InlineData(1, 2)]
        [InlineData(1, 3)]
        [InlineData(2, 3)]
        void Equality(int major, int minor)
        {
            var version = new Version(major, minor, 3, 7);
            var range = new VersionRange(version, version);
            Assert.Equal(range, range);
            Assert.Equal(range.GetHashCode(), range.GetHashCode());
        }

        [Theory]
        [InlineData(11, 0, 15, int.MaxValue, "2012-2017")]
        [InlineData(11, 0, 15, int.MaxValue, "11-15")]
        [InlineData(11, 0, 11, int.MaxValue, "2012")]
        [InlineData(11, 0, 11, int.MaxValue, "11")]
        [InlineData(11, 1, 15, 1, "2012.1-2017.1")]
        [InlineData(11, 1, 15, 1, "2012.1-15.1")]
        [InlineData(11, 1, 15, 1, "11.1-2017.1")]
        [InlineData(11, 1, 11, 1, "2012.1")]
        [InlineData(11, 1, 11, 1, "11.1")]
        void TestAllowedRangeFormats(int expectedMinMajor, int expectedMinMinor, int expectedMaxMajor, int expectedMaxMinor, string input)
        {
            var expectedVersionRange = new VersionRange(new Version(expectedMinMajor, expectedMinMinor), new Version(expectedMaxMajor, expectedMaxMinor));
            var versionRange = new VersionRange(input);
            Assert.Equal(expectedVersionRange, versionRange);
        }
    }
}