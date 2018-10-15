// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Internal
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Vs;
    using VsixTesting.Utilities;
    using VsixTesting.XunitX.Internal.Utilities;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class VsInstanceTestCase : VsTestCaseBase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public VsInstanceTestCase()
        {
        }

        public VsInstanceTestCase(string instanceId, string applicationPath, string rootSuffix, IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, object[] testMethodArguments = null)
            : base(instanceId, diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments)
        {
            ApplicationPath = applicationPath;
            RootSuffix = rootSuffix;
            DebugMixedMode = false;
            ExtensionDirectories = new HashSet<string>();
        }

        public string ApplicationPath { get; private set; }
        public string RootSuffix { get; private set; }
        public bool DebugMixedMode { get; private set; }
        public HashSet<string> ExtensionDirectories { get; private set; }

        public override void Serialize(IXunitSerializationInfo info)
        {
            base.Serialize(info);
            info.AddValue(nameof(ApplicationPath), ApplicationPath);
            info.AddValue(nameof(RootSuffix), RootSuffix);
            info.AddValue(nameof(DebugMixedMode), DebugMixedMode);
            info.AddValue(nameof(ExtensionDirectories), string.Join(";", ExtensionDirectories));
        }

        public override void Deserialize(IXunitSerializationInfo info)
        {
            base.Deserialize(info);
            ApplicationPath = info.GetValue<string>(nameof(ApplicationPath));
            RootSuffix = info.GetValue<string>(nameof(RootSuffix));
            DebugMixedMode = info.GetValue<bool>(nameof(DebugMixedMode));
            ExtensionDirectories = new HashSet<string>(info.GetValue<string>(nameof(ExtensionDirectories)).Split(';'));
        }

        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            => new ExceptionRunner(this, new NotImplementedException(), string.Empty, messageBus, aggregator, cancellationTokenSource).RunAsync();

        public async Task LaunchAndDebug(IMessageBus messageBus, CancellationTokenSource cancellationTokenSource)
        {
            await ThreadUtil.RunOnStaThreadAsync(async () =>
            {
                using (var retryFilter = new RetryMessageFilter())
                {
                    var diagnostics = new VisualDiagnostics(this, DiagnosticMessageSink, messageBus, cancellationTokenSource);
                    var extensionsToInstall = VsInstance.GetExtensionsToInstall(ExtensionDirectories);
                    var installation = VisualStudioUtil.FindInstallations().First(i => i.ApplicationPath == ApplicationPath);
                    var hive = new VsHive(installation, RootSuffix);
                    await VsInstance.Prepare(hive, extensionsToInstall, resetSettings: false, diagnostics, installInvoker: false);
                    var process = await diagnostics.RunAsync("Launching Instance", () => Task.FromResult(VisualStudioUtil.StartProcess(hive)));
                    if (Debugger.IsAttached)
                        await VsInstance.AttachDebugger(process, DebugMixedMode, diagnostics);
                }
            });
        }

        public void MergeSettings(ITestSettings settings)
        {
            ExtensionDirectories.Add(settings.ExtensionsDirectory);
            if (settings.DebugMixedMode)
                DebugMixedMode = true;
        }

        protected override string GetDisplayName(IAttributeInfo factAttribute, string displayName)
            => $"{TraitKey} [{TraitValue}]";

        protected override string GetUniqueID()
            => base.GetUniqueID() + "-" + InstanceId;
    }
}