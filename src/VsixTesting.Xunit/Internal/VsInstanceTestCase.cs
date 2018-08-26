// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Internal
{
    using System;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;
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

        public VsInstanceTestCase(string instanceId, IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, object[] testMethodArguments = null)
            : base(instanceId, diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments)
        {
        }

        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            => new ExceptionRunner(this, new NotImplementedException(), string.Empty, messageBus, aggregator, cancellationTokenSource).RunAsync();

        protected override string GetDisplayName(IAttributeInfo factAttribute, string displayName)
            => $"{TraitKey} [{TraitValue}]";

        protected override string GetUniqueID()
            => base.GetUniqueID() + "-" + InstanceId;
    }
}