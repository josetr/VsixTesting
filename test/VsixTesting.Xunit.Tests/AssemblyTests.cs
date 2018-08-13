// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Tests
{
    using System.Linq;
    using System.Reflection;
    using Xunit;

    public class AssemblyTests
    {
        [Fact]
        void DeployedTestFrameworkExists()
        {
            Assert.NotNull(GetTestFrameworkAttribute());
        }

        [Fact]
        void DeployedTestFrameworkIsValid()
        {
            var testFrameworkTypeName = GetTestFrameworkAttribute().ConstructorArguments[0].Value;
            var assemblyName = GetTestFrameworkAttribute().ConstructorArguments[1].Value;

            Assert.Equal(typeof(Xunit.VsTestFramework).Assembly.GetName().Name, assemblyName);
            Assert.Equal(typeof(Xunit.VsTestFramework).FullName, testFrameworkTypeName);
        }

        static CustomAttributeData GetTestFrameworkAttribute()
        {
            return Assembly.GetExecutingAssembly().CustomAttributes
                .FirstOrDefault(c => c.AttributeType == typeof(TestFrameworkAttribute));
        }
    }
}
