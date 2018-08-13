// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Tests
{
    using System;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Xunit;

    public class ClassFixtureTests : IClassFixture<MyClassFixture>
    {
        private static int classCounter = 0;

        public ClassFixtureTests(MyClassFixture fixture)
            => Assert.Equal(++classCounter, ++fixture.Counter);

        [VsFact]
        void Fact() => Assert.True(true);

        [VsTheory]
        [InlineData(0)]
        void Theory(int zero) => Assert.Equal(0, zero);
    }

    public class MyClassFixture : IDisposable
    {
        public int Counter { get; set; } = 0;

        public MyClassFixture()
        {
            Assert.NotNull(Package.GetGlobalService(typeof(SVsWebBrowsingService)));
        }

        public void Dispose()
        {
        }
    }
}