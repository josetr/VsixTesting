// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Internal
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Vs;
    using VsixTesting;
    using VsixTesting.XunitX.Internal.Utilities;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal sealed class VsInstanceTestAssemblyRunner : IAsyncDisposable
    {
        private static readonly Lazy<IEnumerable<VsInstallation>> Installations
            = new Lazy<IEnumerable<VsInstallation>>(() => VisualStudioUtil.FindInstallations());

        private readonly ITestAssembly testAssembly;
        private readonly IMessageSink diagnosticMessageSink;
        private readonly IMessageSink executionMessageSink;
        private readonly ITestFrameworkExecutionOptions executionOptions;
        private readonly IMessageBus messageBus;

        private readonly ExceptionAggregator aggregator;
        private readonly IEnumerable<VsTestCaseBase> testCases;
        private readonly VsInstanceTestCase instanceTestCase;
        private IDiagnostics diagnostics;
        private VsInstance instance;
        private Rmt rmt;
        private bool initialized;
        private RemoteTestAssemblyRunner remoteTestAssemblyRunner;
        private ConcurrentDictionary<string, byte> startedTestCases = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<string, RunSummary> finishedTestCases = new ConcurrentDictionary<string, RunSummary>();

        public VsInstanceTestAssemblyRunner(ITestAssembly testAssembly, IEnumerable<VsTestCaseBase> testCases, VsInstanceTestCase instanceTestCase, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions, Func<IMessageBus> mesageBusCreator, ExceptionAggregator aggregator)
        {
            this.testCases = testCases;
            this.testAssembly = testAssembly;
            this.instanceTestCase = instanceTestCase;
            this.diagnosticMessageSink = diagnosticMessageSink;
            this.executionMessageSink = executionMessageSink;
            this.executionOptions = executionOptions;
            this.messageBus = mesageBusCreator();
            this.aggregator = aggregator;
            InstanceId = testCases.First().InstanceId;
            var vsTestCases = testCases.OfType<VsTestCase>();
            Settings = vsTestCases.First().Settings;
            Settings.ResetSettings = vsTestCases.Any(c => c.Settings.ResetSettings);
            Settings.DebugMixedMode = vsTestCases.Any(c => c.Settings.DebugMixedMode);
            InstancePath = vsTestCases.First().InstancePath;
        }

        public string InstanceName => $"Visual Studio {InstanceId} Instance";
        public string InstanceId { get; }
        public string InstancePath { get; }
        public VsTestSettings Settings { get; }
        public bool VsResetSettings { get; }
        public bool VsDebugMixedMode { get; }

        public IEnumerable<VsTestCaseBase> RemainingTestCases => testCases.Where(tc => !finishedTestCases.ContainsKey(tc.UniqueID));
        public IEnumerable<VsTestCaseBase> RunningTestCases => RemainingTestCases.Where(tc => startedTestCases.ContainsKey(tc.UniqueID));
        private string TestAssemblyDirectory => Path.GetDirectoryName(testAssembly.Assembly.AssemblyPath);

        public async Task<RunSummary> RunAsync(CancellationTokenSource cts)
        {
            var remoteTestAssemblyRunner = default(RemoteTestAssemblyRunner);
            var runSummary = new RunSummary();

            try
            {
                await Init(cts);
                remoteTestAssemblyRunner = CreateRemoteTestAssemblyRunner(cts);
                runSummary = remoteTestAssemblyRunner.Run();
            }
            catch (Exception e)
            {
                runSummary = CalculateRunSummary();
                ReportRunningTestCases();
                await CancelRemainingTestCases(e, cts);
                throw;
            }
            finally
            {
                remoteTestAssemblyRunner?.Dispose();
            }

            return runSummary;
        }

        public async Task Init(CancellationTokenSource cts)
        {
            if (initialized == true)
                return;

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            diagnostics = CreateDiagnostics(cts);
            if (testCases.OfType<VsTestCase>().GroupBy(c => c.Settings.SecureChannel).Count() >= 2)
                throw new Exception($"All test methods sharing the same Visual Studio Instance must also use the same value for {nameof(ITestSettings.SecureChannel)}.");
            var installation = Installations.Value.First(i => i.ApplicationPath == InstancePath);
            var hive = new VsHive(installation, Settings.RootSuffix);
            await VsInstance.Prepare(hive, GetExtensionsToInstall(), Settings.ResetSettings, diagnostics);
            instance = await VsInstance.Launch(hive, Settings.GetLaunchTimeout(), diagnostics);
            if (Debugger.IsAttached)
                await VsInstance.AttachDebugger(instance.Process, Settings.DebugMixedMode, diagnostics);
            instance.SetAssemblyResolver(TestAssemblyDirectory);
            rmt = instance.GetOrCreateSingletonService<Rmt>("VsixTesting.Xunit", Settings.SecureChannel);
            remoteTestAssemblyRunner = CreateRemoteTestAssemblyRunner(cts);
            initialized = true;
        }

        public async Task DisposeAsync()
        {
            if (diagnostics is IAsyncDisposable disposableDiagnostics)
                await disposableDiagnostics?.DisposeAsync();

            messageBus?.Dispose();
            rmt?.Dispose();

            if (instance != null)
                await instance?.DisposeAsync();

            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
        }

        private IDiagnostics CreateDiagnostics(CancellationTokenSource cancellationTokenSource)
        {
            return instanceTestCase == null
                ? (IDiagnostics)new BasicDiagnostics(diagnosticMessageSink)
                : new VisualDiagnostics(instanceTestCase, diagnosticMessageSink, messageBus, cancellationTokenSource);
        }

        private RemoteTestAssemblyRunner CreateRemoteTestAssemblyRunner(CancellationTokenSource cancellationTokenSource)
        {
            var diagnosticSink = new DynMessageSink(diagnosticMessageSink, message =>
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    return false;

                return diagnosticMessageSink.OnMessage(message);
            });

            var bus = new DynMessageBus(messageBus, message =>
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    return false;

                switch (message)
                {
                    case ITestAssemblyStarting assemblyStarting:
                    case ITestAssemblyFinished assemblyFinished:
                        return true;
                    case ITestCaseStarting testCaseStarting:
                        startedTestCases.TryAdd(testCaseStarting.TestCase.UniqueID, default);
                        break;
                    case ITestCaseFinished testCaseFinished:
                        finishedTestCases[testCaseFinished.TestCase.UniqueID] = testCaseFinished.ToRunSummary();
                        break;
                }

                return messageBus.QueueMessage(message);
            });

            return rmt.CreateTestAssemblyRunner(testAssembly.Assembly.AssemblyPath, testAssembly.ConfigFileName, testCases.ToArray(), diagnosticSink, null, executionOptions, bus);
        }

        private void ReportRunningTestCases()
        {
            if (diagnostics != null && RunningTestCases.Count() > 0)
            {
                diagnostics.WriteLine("The following test cases were running when the instance exited:");
                RunningTestCases.ToList().ForEach(tc => diagnostics.WriteLine(tc.DisplayName));
            }
        }

        private async Task CancelRemainingTestCases(Exception e, CancellationTokenSource cts)
        {
            e.Demystify();
            var errorTestCases = RunningTestCases.ToList();
            if (!errorTestCases.Any() && RemainingTestCases.Any())
                errorTestCases.Add(RemainingTestCases.First());
            var errorOutput = diagnostics?.ToString() ?? string.Empty;
            await Task.WhenAll(errorTestCases.Select(tc => new ExceptionRunner(tc, e, errorOutput, messageBus, aggregator, cts).RunAsync()));

            var exception = new Exception("Test did not run because an exception was thrown from the test assembly runner", e);
            var skipTestCases = RemainingTestCases.Except(errorTestCases);
            await Task.WhenAll(skipTestCases.Select(tc => new XunitTestCaseRunner(tc, tc.DisplayName, exception.Message, null, tc.TestMethodArguments, messageBus, aggregator, cts).RunAsync()));
        }

        private RunSummary CalculateRunSummary()
        {
            return new RunSummary()
            {
                Total = finishedTestCases.Sum(ftrs => ftrs.Value.Total),
                Failed = finishedTestCases.Sum(ftrs => ftrs.Value.Failed),
                Skipped = finishedTestCases.Sum(ftrs => ftrs.Value.Skipped),
                Time = finishedTestCases.Sum(ftrs => ftrs.Value.Time),
            };
        }

        private IEnumerable<string> GetExtensionsToInstall()
            => VsInstance.GetExtensionsToInstall(testCases.OfType<VsTestCase>().Select(tc => tc.Settings.ExtensionsDirectory));

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
            => AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.FullName.Equals(args.Name));

        private class DynMessageSink : LongLivedMarshalByRefObject, IMessageSink
        {
            private readonly IMessageSink messageSink;

            public DynMessageSink(IMessageSink messageSink, Func<IMessageSinkMessage, bool> onMessage)
            {
                this.messageSink = messageSink;
                OnMessageCallback = onMessage;
            }

            public Func<IMessageSinkMessage, bool> OnMessageCallback { get; }

            public bool OnMessage(IMessageSinkMessage message)
                => OnMessageCallback(message);
        }

        private class DynMessageBus : LongLivedMarshalByRefObject, IMessageBus
        {
            private readonly IMessageBus messageBus;

            public DynMessageBus(IMessageBus messageSink, Func<IMessageSinkMessage, bool> onMessage)
            {
                this.messageBus = messageSink;
                OnMessageCallback = onMessage;
            }

            public Func<IMessageSinkMessage, bool> OnMessageCallback { get; }

            public void Dispose()
            {
            }

            public bool QueueMessage(IMessageSinkMessage message)
                => OnMessageCallback(message);
        }

        private sealed class Rmt : LongLivedMarshalByRefObject, IDisposable
        {
            public RemoteTestAssemblyRunner CreateTestAssemblyRunner(string testAssemblyPath, string testAssemblyConfigurationFile, VsTestCaseBase[] testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions, IMessageBus messageBus)
            {
                var testAssembly = new TestAssembly(new ReflectionAssemblyInfo(Assembly.LoadFrom(testAssemblyPath)), testAssemblyConfigurationFile);
                return new RemoteTestAssemblyRunner(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions, messageBus);
            }

            public void Dispose()
            {
                Remote.Dispose();
                DisconnectAll();
            }
        }
    }
}