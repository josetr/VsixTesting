// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Tests.Utilities
{
    using System;
    using System.Linq;
    using Xunit.Sdk;

    class Util
    {
        internal static TestMethod CreateTestMethod(Type @class, string name)
        {
            var assemblyInfo = new ReflectionAssemblyInfo(@class.Assembly);
            var testAssembly = new TestAssembly(assemblyInfo);
            var testCollection = new TestCollection(testAssembly, null, string.Empty);
            var classInfo = new ReflectionTypeInfo(@class);
            var testClass = new TestClass(testCollection, classInfo);
            var method = testClass.Class.GetMethods(true).First(m => m.Name == name);
            var testMethod = new TestMethod(testClass, method);
            return testMethod;
        }
    }
}
