// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Vs.Tests
{
    using System;
    using Xunit;

    public class VisualStudioUtilTests
    {
        [Fact]
        void ThrowOnBadProcessNameWorks()
        {
            VisualStudioUtil.ThrowOnBadProcessName("DEVenv");
            Assert.Throws<ArgumentException>(() => VisualStudioUtil.ThrowOnBadProcessName("devenv2"));
            Assert.Throws<NotSupportedException>(() => VisualStudioUtil.ThrowOnBadProcessName("wdexpress"));
        }
    }
}
