// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace VsixTesting.XunitX.Internal
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class BasicDiagnostics : IDiagnostics
    {
        private IMessageSink diagnosticMessageSink;
        private int id = 0;

        internal BasicDiagnostics(IMessageSink diagnosticMessageSink)
            => this.diagnosticMessageSink = diagnosticMessageSink;

        private IOutput Output { get; } = new DiagnosticOutput();

        public void WriteLine(string message)
        {
            diagnosticMessageSink.OnMessage(new DiagnosticMessage(message));
            Output.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
            => WriteLine(string.Format(format, args));

        public Task<T> RunAsync<T>(string displayName, Func<IOutput, Task<T>> work)
        {
            displayName = Interlocked.Increment(ref id) + ". " + displayName;
            WriteLine(displayName);
            return work(Output);
        }

        public override string ToString() => Output.ToString();
    }

    internal class VisualDiagnostics : IDiagnostics, IAsyncDisposable
    {
        private readonly BasicDiagnostics basicDiagnostics;
        private readonly IXunitTestCase testCase;
        private readonly DiagnosticTestCaseRunner testCaseRunner;
        private readonly Task testCaseRunTask;
        private readonly IMessageBus messageBus;
        private readonly CancellationTokenSource cancellationTokenSource;
        private int didFinish = 0;
        private int n = 0;

        public VisualDiagnostics(IXunitTestCase testCase, IMessageSink diagnosticMessageSink, IMessageBus messageBus, CancellationTokenSource cancellationTokenSource)
        {
            this.testCase = testCase;
            this.messageBus = messageBus;
            this.cancellationTokenSource = cancellationTokenSource;
            basicDiagnostics = new BasicDiagnostics(diagnosticMessageSink);
            testCaseRunner = new DiagnosticTestCaseRunner(testCase);
            testCaseRunTask = testCaseRunner.RunAsync(messageBus, cancellationTokenSource);
        }

        public void WriteLine(string message)
            => basicDiagnostics.WriteLine(message);

        public void WriteLine(string format, params object[] args)
            => basicDiagnostics.WriteLine(format, args);

        public Task<T> RunAsync<T>(string displayName, Func<IOutput, Task<T>> func)
        {
            displayName = Interlocked.Increment(ref n) + ". " + displayName;
            WriteLine(displayName);

            if (Interlocked.CompareExchange(ref didFinish, 0, 0) == 1)
                return func(this);

            var funcTcs = new TaskCompletionSource<T>();
            var test = new XunitTest(testCase, displayName);
            var testRunner = new DiagnosticTestRunner<T>(test, func, funcTcs, this);
            testCaseRunner.AddTestTask(testRunner.RunAsync(messageBus, cancellationTokenSource));
            return funcTcs.Task;
        }

        public async Task DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref didFinish, 1, 0) == 1)
                return;

            testCaseRunner.Finish();
            await testCaseRunTask;
        }

        public override string ToString() => basicDiagnostics.ToString();
    }

    internal class DiagnosticTestCaseRunner
    {
        private readonly ConcurrentBag<Task<decimal>> testTasks = new ConcurrentBag<Task<decimal>>();
        private readonly TaskCompletionSource<bool> finishTcs = new TaskCompletionSource<bool>();
        private readonly IXunitTestCase testCase;

        public DiagnosticTestCaseRunner(IXunitTestCase testCase)
        {
            this.testCase = testCase;
        }

        public void AddTestTask(Task<decimal> task)
            => testTasks.Add(task);

        public async Task RunAsync(IMessageBus messageBus, CancellationTokenSource cancellationTokenSource)
        {
            var summary = new RunSummary();

            if (!messageBus.QueueMessage(new TestCaseStarting(testCase)))
                cancellationTokenSource.Cancel();
            else
            {
                try
                {
                    await finishTcs.Task;

                    foreach (Task<decimal> testTask in testTasks)
                    {
                        summary.Total++;
                        try
                        {
                            summary.Time += await testTask;
                        }
                        catch
                        {
                            summary.Failed++;
                        }
                    }
                }
                finally
                {
                    if (!messageBus.QueueMessage(new TestCaseFinished(testCase, summary.Time, summary.Total, summary.Failed, summary.Skipped)))
                        cancellationTokenSource.Cancel();
                }
            }
        }

        public void Finish()
            => finishTcs.SetResult(true);
    }

    internal class DiagnosticTestRunner<T>
    {
        private readonly ITest test;
        private readonly Func<IOutput, Task<T>> func;
        private readonly TaskCompletionSource<T> funcTcs;
        private readonly IOutput output;

        public DiagnosticTestRunner(ITest test, Func<IOutput, Task<T>> func, TaskCompletionSource<T> funcTcs, IOutput output)
        {
            this.test = test;
            this.func = func;
            this.funcTcs = funcTcs;
            this.output = output;
        }

        public async Task<decimal> RunAsync(IMessageBus messageBus, CancellationTokenSource cancellationTokenSource)
        {
            var timer = new ExecutionTimer();
            var testOutput = new TestOutputHelperAdapter();
            testOutput.Initialize(messageBus, test);

            if (!messageBus.QueueMessage(new TestStarting(test)))
            {
                cancellationTokenSource.Cancel();
                funcTcs.SetCanceled();
            }
            else
            {
                try
                {
                    await timer.AggregateAsync(async () =>
                    {
                        funcTcs.SetResult(await func(new MultiOutput(new[] { testOutput, output })));
                    });

                    if (!messageBus.QueueMessage(new TestPassed(test, timer.Total, testOutput.Output)))
                        cancellationTokenSource.Cancel();
                }
                catch (Exception exception)
                {
                    funcTcs.SetException(exception);
                    if (!messageBus.QueueMessage(new TestFailed(test, timer.Total, testOutput.Output, exception)))
                        cancellationTokenSource.Cancel();
                }
                finally
                {
                    if (!messageBus.QueueMessage(new TestFinished(test, timer.Total, testOutput.Output)))
                        cancellationTokenSource.Cancel();
                }
            }

            return timer.Total;
        }

        private class TestOutputHelperAdapter : TestOutputHelper, IOutput
        {
        }
    }
}
