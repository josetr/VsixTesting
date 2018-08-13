// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Common.Tests
{
    using System;
    using Xunit;

    public class VersionExtensions
    {
        [Theory]
        [InlineData(11, 1, 11, 0)]
        [InlineData(int.MaxValue, 2, int.MaxValue, 1)]
        [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue - 1)]
        [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)]
        void IncreaseMinorVersion(int expectedMajor, int expectedMinor, int major, int minor)
            => Assert.Equal(new Version(expectedMajor, expectedMinor), new Version(major, minor).IncreaseMinorVersion());

        [Theory]
        [InlineData(10, int.MaxValue, 11, 0)]
        [InlineData(0, 0, 0, 1)]
        [InlineData(0, 0, 0, 0)]
        void DecreaseMinorVersion(int expectedMajor, int expectedMinor, int major, int minor)
            => Assert.Equal(new Version(expectedMajor, expectedMinor), new Version(major, minor).DecreaseMinorVersion());
    }
}
