// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class ProjectReferenceTests
    {
        const bool CopyVsixDefaultValue = true;

        [Theory]
        [InlineData(null, "CopyVsix/Default")]
        [InlineData(true, "CopyVsix", "True.Relative")]
        [InlineData(true, "CopyVsix", "True.Absolute")]
        [InlineData(false, "CopyVsix", "False")]
        void CopyVsix(bool? copyVsix, params string[] paths)
        {
            var path = Path.Combine(paths.Concat(new[] { "VsixTesting.Invoker.vsix" }).ToArray());
            Assert.Equal(copyVsix.GetValueOrDefault(CopyVsixDefaultValue), File.Exists(path));
        }
    }
}