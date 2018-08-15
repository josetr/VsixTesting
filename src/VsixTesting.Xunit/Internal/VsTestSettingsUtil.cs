// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Internal
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using VsixTesting;
    using Xunit;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class VsTestSettingsUtil
    {
        public static VsTestSettings FromTestMethod(ITestMethod testMethod)
        {
            if (testMethod == null)
                throw new ArgumentNullException(nameof(TestMethod));

            var defaultDirectory = Path.GetDirectoryName(testMethod.TestClass.TestCollection.TestAssembly.Assembly.AssemblyPath);
            var defaults = VsTestSettings.Defaults;

            return new VsTestSettings()
            {
                Version = GetTestAttributeArgument(
                    nameof(ITestSettings.Version), defaults.Version),

                RootSuffix = GetTestAttributeArgument(
                    nameof(ITestSettings.RootSuffix), defaults.RootSuffix),

                DebugMixedMode = GetTestAttributeArgument(
                    nameof(ITestSettings.DebugMixedMode), defaults.DebugMixedMode),

                SecureChannel = GetTestAttributeArgument(
                    nameof(ITestSettings.SecureChannel), defaults.SecureChannel),

                AllowPreview = GetTestAttributeArgument(
                    nameof(ITestSettings.AllowPreview), defaults.AllowPreview),

                ExtensionsDirectory = NormalizeDirectory(
                    GetTestAttributeArgument(
                        nameof(ITestSettings.ExtensionsDirectory),
                        defaults.ExtensionsDirectory)),

                ScreenshotsDirectory = NormalizeDirectory(
                    GetTestAttributeArgument(
                        nameof(ITestSettings.ScreenshotsDirectory),
                        defaults.ScreenshotsDirectory)),

                UIThread = GetTestAttributeArgument(
                    nameof(ITestSettings.UIThread), defaults.UIThread),

                ReuseInstance = GetTestAttributeArgument(
                    nameof(ITestSettings.ReuseInstance), defaults.ReuseInstance),

                TakeScreenshotOnFailure = GetTestAttributeArgument(
                    nameof(ITestSettings.TakeScreenshotOnFailure), defaults.TakeScreenshotOnFailure),
            };

            TValue GetTestAttributeArgument<TValue>(string argumentName, TValue defaultValue)
            {
                var methodAttr = testMethod.Method.GetCustomAttributes(typeof(ITestSettings)).FirstOrDefault();
                if (methodAttr?.GetNamedArgument<object>(argumentName) is TValue methodValue && IsNamedArg(methodAttr))
                    return methodValue;

                var classAttr = testMethod.TestClass.Class.GetCustomAttributes(typeof(ITestSettings)).FirstOrDefault();
                if (classAttr?.GetNamedArgument<object>(argumentName) is TValue classValue && IsNamedArg(classAttr))
                    return classValue;

                var collectionAttr = testMethod.TestClass.TestCollection.CollectionDefinition?.GetCustomAttributes(typeof(ITestSettings)).FirstOrDefault();
                if (collectionAttr?.GetNamedArgument<object>(argumentName) is TValue collectionValue && IsNamedArg(collectionAttr))
                    return collectionValue;

                var assemblyAttr = testMethod.TestClass.Class.Assembly.GetCustomAttributes(typeof(ITestSettings)).FirstOrDefault();
                if (assemblyAttr?.GetNamedArgument<object>(argumentName) is TValue assemblyValue && IsNamedArg(assemblyAttr))
                    return assemblyValue;

                return defaultValue;

                bool IsNamedArg(IAttributeInfo attributeInfo)
                {
                    if (attributeInfo is ReflectionAttributeInfo reflectionAttributeInfo)
                        return reflectionAttributeInfo.AttributeData.NamedArguments.Any(arg => arg.MemberName == argumentName);
                    return false;
                }
            }

            string NormalizeDirectory(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    return defaultDirectory;

                if (Path.IsPathRooted(path))
                    return path;

                return Path.Combine(defaultDirectory, path);
            }
        }
    }
}