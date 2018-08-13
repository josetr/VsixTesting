// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX
{
    using System.Collections.Generic;
    using VsixTesting.XunitX.Internal;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class VsTheoryDiscoverer : TheoryDiscoverer
    {
        public VsTheoryDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object[] dataRow)
            => VsTestCaseFactory.CreateTestCases(testMethod, dataRow, discoveryOptions.MethodDisplayOrDefault(), DiagnosticMessageSink);

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForSkip(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, string skipReason)
              => VsTestCaseFactory.CreateTestCases(testMethod, null, discoveryOptions.MethodDisplayOrDefault(), DiagnosticMessageSink);

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForSkippedDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object[] dataRow, string skipReason)
            => VsTestCaseFactory.CreateSkippedDataRowTestCases(testMethod, discoveryOptions.MethodDisplayOrDefault(), DiagnosticMessageSink, dataRow, skipReason);

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
            => VsTestCaseFactory.CreateTheoryTestCases(testMethod, discoveryOptions.MethodDisplayOrDefault(), DiagnosticMessageSink);
    }
}