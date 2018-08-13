// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.
#pragma warning disable SA1202 // Elements should be ordered by access

namespace VsixTesting.XunitX.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using VsixTesting.XunitX.Internal.Utilities;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal sealed class RemoteTestAssemblyRunner : LongLivedMarshalByRefObject, IDisposable
    {
        private readonly XunitTestAssemblyRunner runner;

        public RemoteTestAssemblyRunner(ITestAssembly testAssembly, IEnumerable<VsTestCaseBase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions, IMessageBus messageBus)
        {
            runner = new RealTestAssemblyRunner(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions, messageBus);
        }

        public SerializableRunSummary Run()
        {
            var result = runner.RunAsync().GetAwaiter().GetResult();
            return new SerializableRunSummary
            {
                Total = result.Total,
                Failed = result.Failed,
                Skipped = result.Skipped,
                Time = result.Time,
            };
        }

        public void Dispose() => runner.Dispose();

        private class RealTestAssemblyRunner : XunitTestAssemblyRunner
        {
            private readonly IMessageBus messageBus;

            public RealTestAssemblyRunner(ITestAssembly testAssembly, IEnumerable<VsTestCaseBase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions, IMessageBus messageBus)
                : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
            {
                this.messageBus = messageBus;
            }

            protected override IMessageBus CreateMessageBus()
            {
                return messageBus;
            }

            protected override Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
            {
                testCases = testCases.Cast<VsTestCaseBase>().Select(tc => VsTestCaseBase.DeserializeFromString(tc.SerializeToString()));
                return base.RunTestCollectionAsync(messageBus, testCollection, testCases, cancellationTokenSource);
            }
        }
    }
}