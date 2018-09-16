// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Internal.Utilities
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Sdk;

    internal class ExceptionRunner : TestCaseRunner<IXunitTestCase>
    {
        private readonly Exception exception;
        private readonly string[] exceptionTypes;
        private readonly string[] exceptionMessages;
        private readonly string[] exceptionStackTraces;
        private readonly int[] exceptionParenIndices;
        private readonly string output;

        public ExceptionRunner(IXunitTestCase testCase, Exception exception, string output, IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(testCase, messageBus, aggregator, cancellationTokenSource)
        {
            this.exception = exception;
            this.output = output;
        }

        public ExceptionRunner(IXunitTestCase testCase, string[] exceptionTypes, string[] exceptionMessages, string[] exceptionStackTraces, int[] exceptionParenIndices, string output, IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(testCase, messageBus, aggregator, cancellationTokenSource)
        {
            this.exceptionTypes = exceptionTypes;
            this.exceptionMessages = exceptionMessages;
            this.exceptionStackTraces = exceptionStackTraces;
            this.exceptionParenIndices = exceptionParenIndices;
            this.output = output;
        }

        protected override Task<RunSummary> RunTestAsync()
        {
            var test = new XunitTest(TestCase, TestCase.DisplayName);
            var summary = new RunSummary { Total = 1 };

            if (!MessageBus.QueueMessage(new TestStarting(test)))
                CancellationTokenSource.Cancel();
            else
            {
                if (!string.IsNullOrEmpty(TestCase.SkipReason))
                {
                    summary.Skipped = 1;

                    if (!MessageBus.QueueMessage(new TestSkipped(test, TestCase.SkipReason)))
                        CancellationTokenSource.Cancel();
                }
                else
                {
                    summary.Failed = 1;

                    if (!MessageBus.QueueMessage(exception != null
                        ? new TestFailed(test, 0, output, exception)
                        : new TestFailed(test, 0, output, exceptionTypes, exceptionMessages, exceptionStackTraces, exceptionParenIndices)))
                    {
                        CancellationTokenSource.Cancel();
                    }
                }

                if (!MessageBus.QueueMessage(new TestFinished(test, 0, output)))
                    CancellationTokenSource.Cancel();
            }

            return Task.FromResult(summary);
        }
    }
}