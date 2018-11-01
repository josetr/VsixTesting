// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.
#pragma warning disable SA1402 // File may only contain a single type

namespace VsixTesting.XunitX.Internal
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Vs;
    using VsixTesting.Utilities;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class VsTestCaseFactory
    {
        private static ConcurrentDictionary<string, Instance> Instances { get; }
            = new ConcurrentDictionary<string, Instance>();

        private static Lazy<IEnumerable<VsInstallation>> Installations { get; }
            = new Lazy<IEnumerable<VsInstallation>>(() => VisualStudioUtil.FindInstallations());

        private static Lazy<Process> ParentVsProcess { get; }
            = new Lazy<Process>(() => ProcessUtil.TryGetParentProcess(Process.GetCurrentProcess(), p => p.ProcessName.Equals(VisualStudioUtil.ProcessName, StringComparison.OrdinalIgnoreCase)));

        public static IEnumerable<IXunitTestCase> CreateTheoryTestCases(ITestMethod testMethod, TestMethodDisplay testMethodDisplay, IMessageSink diagnosticMessageSink)
        {
            Xunit.VsTestFramework.ThrowIfNotInUse();
            var testSettings = VsTestSettingsUtil.FromTestMethod(testMethod);
            foreach (var instance in GetInstances(Installations.Value, testMethod, testSettings, Instances))
            {
                var testCase = new VsTheoryTestCase(instance.Id, instance.Path, testSettings, diagnosticMessageSink, testMethodDisplay, instance.CreateTestMethod(testMethod), null);
                yield return testCase;

                if (string.IsNullOrEmpty(testCase.SkipReason) && instance.DidReportInstanceTestCase == false)
                    yield return instance.TestCase = new VsInstanceTestCase(instance.Id, instance.Path, testSettings.RootSuffix, diagnosticMessageSink, testMethodDisplay, instance.CreateVisualStudioTestMethod());
                instance.TestCase?.MergeSettings(testCase.Settings);
            }
        }

        public static IEnumerable<IXunitTestCase> CreateTestCases(ITestMethod testMethod, object[] testMethodArguments, TestMethodDisplay testMethodDisplay, IMessageSink diagnosticMessageSink)
        {
            Xunit.VsTestFramework.ThrowIfNotInUse();
            var testSettings = VsTestSettingsUtil.FromTestMethod(testMethod);
            foreach (var instance in GetInstances(Installations.Value, testMethod, testSettings, Instances))
            {
                var testCase = new VsTestCase(instance.Id, instance.Path, testSettings, diagnosticMessageSink, testMethodDisplay, instance.CreateTestMethod(testMethod), testMethodArguments);
                yield return testCase;

                if (string.IsNullOrEmpty(testCase.SkipReason) && instance.DidReportInstanceTestCase == false)
                    yield return instance.TestCase = new VsInstanceTestCase(instance.Id, instance.Path, testSettings.RootSuffix, diagnosticMessageSink, testMethodDisplay, instance.CreateVisualStudioTestMethod());
                instance.TestCase?.MergeSettings(testCase.Settings);
            }
        }

        internal static IEnumerable<IXunitTestCase> CreateSkippedDataRowTestCases(ITestMethod testMethod, TestMethodDisplay testMethodDisplay, IMessageSink diagnosticMessageSink, object[] dataRow, string skipReason)
        {
            Xunit.VsTestFramework.ThrowIfNotInUse();
            var testSettings = VsTestSettingsUtil.FromTestMethod(testMethod);
            foreach (var instance in GetInstances(Installations.Value, testMethod, testSettings, Instances))
                yield return new VsSkippedDataRowTestCase(instance.Id, diagnosticMessageSink, testMethodDisplay, testMethod, skipReason, dataRow);
        }

        internal static IEnumerable<Instance> GetInstances(IEnumerable<VsInstallation> installations, ITestMethod testMethod, VsTestSettings testSettings, ConcurrentDictionary<string, Instance> instances)
        {
            var output = new StringBuilder();
            installations = FilterInstallations(installations, testSettings, output, ParentVsProcess.Value?.MainModule?.FileName);

            if (!installations.Any())
                throw new InvalidOperationException("Cannot find a viable Visual Studio Instance for the specified test case.\r\n" + output);

            foreach (var installation in installations)
            {
                var instanceId = $"{VersionUtil.GetYear(installation.Version)} {testSettings.RootSuffix}";
                if (!testSettings.ReuseInstance)
                {
                    var fullMethodName = testMethod.TestClass.Class.ToRuntimeType() + testMethod.Method.Name;
                    instanceId += string.Format(" {0:X}", fullMethodName.GetHashCode()).ToLower();
                }

                yield return instances.GetOrAdd(instanceId, id => new Instance(id, installation.ApplicationPath, testMethod.TestClass.TestCollection.TestAssembly));
            }
        }

        internal static IEnumerable<VsInstallation> FilterInstallations(IEnumerable<VsInstallation> installations, VsTestSettings settings, StringBuilder output = null, string preferedAppPath = null)
        {
            foreach (var group in installations.GroupBy(i => i.Version.Major).OrderBy(g => g.Key))
            {
                foreach (var installation in group
                    .OrderBy(i => !i.ApplicationPath.Equals(preferedAppPath, StringComparison.OrdinalIgnoreCase))
                    .ThenBy(i => i.Preview))
                {
                    if (!settings.SupportedVersionRanges.Any(range => installation.Version >= range.Minimum && installation.Version <= range.Maximum))
                    {
                        output?.AppendLine($"Skipping {installation.Path} because the version {installation.Version} is not within any specified version range {string.Join(";", settings.SupportedVersionRanges)}.");
                        continue;
                    }

                    yield return installation;
                    break;
                }
            }
        }

        internal class Instance
        {
            private readonly ITestAssembly testAssembly;
            private ConcurrentDictionary<Guid, TestCollection> testCollections = new ConcurrentDictionary<Guid, TestCollection>();
            private int reportInstanceTestCaseOnce = 0;
            private static TestCollection instancesTestCollection;

            public Instance(string id, string path, ITestAssembly testAssembly)
            {
                Id = id;
                Path = path;
                this.testAssembly = testAssembly;
            }

            public string Id { get; }
            public string Path { get; }
            public bool DidReportInstanceTestCase => Interlocked.CompareExchange(ref reportInstanceTestCaseOnce, 1, 0) == 1 ? true : false;
            public VsInstanceTestCase TestCase { get; set; }

            public ITestMethod CreateTestMethod(ITestMethod testMethod)
            {
                var testCollection = testMethod.TestClass.TestCollection;
                var newTestCollectionDisplayName = $"{testCollection.DisplayName} - {Id}";
                var newTestCollection = testCollections.GetOrAdd(testCollection.UniqueID, _ => new TestCollection(testCollection.TestAssembly, testCollection.CollectionDefinition, newTestCollectionDisplayName));
                var newTestMethod = new TestMethod(new TestClass(newTestCollection, testMethod.TestClass.Class), testMethod.Method);
                return newTestMethod;
            }

            public ITestMethod CreateVisualStudioTestMethod()
            {
                var oldTc = Interlocked.CompareExchange(ref instancesTestCollection, new TestCollection(testAssembly, null, "Instances"), null);
                var testClass = new TestClass(instancesTestCollection, new ReflectionTypeInfo(typeof(VsixTesting.Instances)));
                var method = testClass.Class.GetMethods(false).First(m => m.Name == nameof(VsixTesting.Instances.VisualStudio));
                return new TestMethod(testClass, method);
            }
        }
    }
}
