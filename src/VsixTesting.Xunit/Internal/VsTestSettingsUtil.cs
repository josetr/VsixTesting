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
                VsVersion = GetInstanceArgument(
                    nameof(IInstanceSettings.Version), defaults.VsVersion),

                VsRootSuffix = GetInstanceArgument(
                    nameof(IInstanceSettings.RootSuffix), defaults.VsRootSuffix),

                VsDebugMixedMode = GetInstanceArgument(
                    nameof(IInstanceSettings.DebugMixedMode), defaults.VsDebugMixedMode),

                VsSecureChannel = GetInstanceArgument(
                    nameof(IInstanceSettings.SecureChannel), defaults.VsSecureChannel),

                VsAllowPreview = GetInstanceArgument(
                    nameof(IInstanceSettings.AllowPreview), defaults.VsAllowPreview),

                VsExtensionsDirectory = NormalizeDirectory(
                    GetInstanceArgument(
                        nameof(IInstanceSettings.ExtensionsDirectory),
                        defaults.VsExtensionsDirectory)),

                ScreenshotsDirectory = NormalizeDirectory(
                    GetInstanceArgument(
                        nameof(IInstanceSettings.ScreenshotsDirectory),
                        defaults.ScreenshotsDirectory)),

                UIThread = GetTestAttributeArgument(
                    nameof(ITestSettings.UIThread), defaults.UIThread),

                ReuseInstance = GetTestAttributeArgument(
                    nameof(ITestSettings.ReuseInstance), defaults.ReuseInstance),

                TakeScreenshotOnFailure = GetTestAttributeArgument(
                    nameof(ITestSettings.TakeScreenshotOnFailure), defaults.TakeScreenshotOnFailure),
            };

            TValue GetTestAttributeArgument<TValue>(string argumentName, TValue defaultValue)
                => GetAttributeArgument<TValue, ITestSettings, ITestSettings, ITestSettings>(
                    argumentName, defaultValue);

            TValue GetInstanceArgument<TValue>(string argumentName, TValue defaultValue)
                => GetAttributeArgument<TValue, IInstanceSettings, IInstanceSettings, IInstanceSettings>(
                    argumentName, defaultValue);

            TValue GetAttributeArgument<TValue, TMethodAttrType, TClassAttrType, TAssemblyAttrType>(string argumentName, TValue defaultValue)
            {
                var methodAttr = testMethod.Method.GetCustomAttributes(typeof(TMethodAttrType)).FirstOrDefault();
                if (methodAttr?.GetNamedArgument<object>(argumentName) is TValue methodValue && IsNamedArg(methodAttr))
                    return methodValue;

                var classAttr = testMethod.TestClass.Class.GetCustomAttributes(typeof(TClassAttrType)).FirstOrDefault();
                if (classAttr?.GetNamedArgument<object>(argumentName) is TValue classValue && IsNamedArg(classAttr))
                    return classValue;

                var collectionAttr = testMethod.TestClass.TestCollection.CollectionDefinition?.GetCustomAttributes(typeof(TClassAttrType)).FirstOrDefault();
                if (collectionAttr?.GetNamedArgument<object>(argumentName) is TValue collectionValue && IsNamedArg(collectionAttr))
                    return collectionValue;

                var assemblyAttr = testMethod.TestClass.Class.Assembly.GetCustomAttributes(typeof(TAssemblyAttrType)).FirstOrDefault();
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