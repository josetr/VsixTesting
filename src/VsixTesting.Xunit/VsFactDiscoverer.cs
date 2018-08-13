// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VsixTesting.XunitX.Internal;
    using VsixTesting.XunitX.Internal.Utilities;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class VsFactDiscoverer : FactDiscoverer
    {
        public VsFactDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        public override IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            var results = new List<IXunitTestCase>();

            if (testMethod.Method.GetParameters().Any())
                results.Add(new ExecutionErrorTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, "[VsFact] methods are not allowed to have parameters. Did you mean to use [VsTheory]?"));
            else if (testMethod.Method.IsGenericMethodDefinition)
                results.Add(new ExecutionErrorTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, "[VsFact] methods are not allowed to be generic."));
            else
            {
                try
                {
                    results.AddRange(VsTestCaseFactory.CreateTestCases(testMethod, null, discoveryOptions.MethodDisplayOrDefault(), DiagnosticMessageSink));
                }
                catch (Exception exception)
                {
                    results.Add(new ExceptionTestCase(exception, DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod));
                }
            }

            return results;
        }
    }
}