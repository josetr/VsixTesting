// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Tests
{
    using VsixTesting;
    using Xunit;

    public class InstancesTests
    {
        [Fact]
        public void VisualStudioTest()
        {
            new Instances().VisualStudio();
        }
    }
}
