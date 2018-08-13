// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Tests
{
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Xunit;

    public class CollectionFixtureTests
    {
        private static int counter = 0;

        [Collection("CollectionName")]
        public class First
        {
            public First(TestCollectionFixture collectionFixture)
            {
                Assert.Equal(++counter, ++collectionFixture.Counter);
            }

            [VsFact]
            void Fact() => Assert.True(true);

            [VsTheory]
            [InlineData(0)]
            void Theory(int zero) => Assert.Equal(0, zero);
        }

        [Collection("CollectionName")]
        public class Second
        {
            public Second(TestCollectionFixture collectionFixture)
            {
                Assert.Equal(++counter, ++collectionFixture.Counter);
            }

            [VsFact]
            void Fact() => Assert.True(true);

            [VsTheory]
            [InlineData(0)]
            void Theory(int zero) => Assert.Equal(0, zero);
        }
    }

    [CollectionDefinition("CollectionName")]
    public class TestCollectionFixtureDefinition : ICollectionFixture<TestCollectionFixture>
    {
    }

    public class TestCollectionFixture
    {
        public int Counter { get; set; } = 0;

        public TestCollectionFixture()
            => Assert.NotNull(Package.GetGlobalService(typeof(SVsWebBrowsingService)));
    }
}
