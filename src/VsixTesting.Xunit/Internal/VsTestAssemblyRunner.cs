// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Internal
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using VsixTesting.Utilities;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class VsTestAssemblyRunner : XunitTestAssemblyRunner
    {
        public VsTestAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
            : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
        {
        }

        private IEnumerable<IXunitTestCase> LocalTestCases => TestCases.Except(TestCases.OfType<VsTestCaseBase>());
        private IEnumerable<VsTestCaseBase> RemoteTestCases => TestCases.OfType<VsTestCaseBase>().Except(TestCases.OfType<VsInstanceTestCase>());

        protected override async Task<RunSummary> RunTestCollectionsAsync(IMessageBus messageBus, CancellationTokenSource cancellationTokenSource)
        {
            var result = await Local_RunTestCasesAsync(LocalTestCases, messageBus, cancellationTokenSource);

            foreach (var remoteTestCases in RemoteTestCases.GroupBy(tc => tc.InstanceId).OrderBy(g => g.Key))
                result.Aggregate(await Remote_RunTestCasesAsync(remoteTestCases.Key, remoteTestCases, messageBus, cancellationTokenSource));

            foreach (var instanceTestCase in TestCases.OfType<VsInstanceTestCase>())
            {
                if (!RemoteTestCases.Any(tc => tc.InstanceId == instanceTestCase.InstanceId))
                    await instanceTestCase.LaunchAndDebug(messageBus, cancellationTokenSource);
            }

            return result;
        }

        private async Task<RunSummary> Local_RunTestCasesAsync(IEnumerable<IXunitTestCase> testCases, IMessageBus messageBus, CancellationTokenSource cancellationTokenSource)
        {
            var allTestCases = TestCases;
            TestCases = testCases;
            var result = await base.RunTestCollectionsAsync(messageBus, cancellationTokenSource);
            TestCases = allTestCases;
            return result;
        }

        private async Task<RunSummary> Remote_RunTestCasesAsync(string instanceId, IEnumerable<VsTestCaseBase> testCases, IMessageBus messageBus, CancellationTokenSource cancellationTokenSource)
        {
            if (testCases.All(tc => !string.IsNullOrEmpty(tc.SkipReason)))
                return await Local_RunTestCasesAsync(testCases, messageBus, cancellationTokenSource);

            var instanceTestCase = TestCases.OfType<VsInstanceTestCase>().FirstOrDefault(tc => tc.InstanceId == instanceId);

            return await ThreadUtil.RunOnStaThreadAsync(async () =>
            {
                var runner = new VsInstanceTestAssemblyRunner(TestAssembly, testCases, instanceTestCase, DiagnosticMessageSink, ExecutionMessageSink, ExecutionOptions, () => CreateMessageBus(), Aggregator);

                try
                {
                    using (var retryFilter = new RetryMessageFilter())
                    {
                        return await runner.RunAsync(cancellationTokenSource);
                    }
                }
                finally
                {
                    await runner.DisposeAsync();
                }
            });
        }
    }
}
