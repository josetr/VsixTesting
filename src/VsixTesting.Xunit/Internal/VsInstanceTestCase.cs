// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Internal
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;
    using VsixTesting.XunitX.Internal.Utilities;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class VsInstanceTestCase : XunitTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public VsInstanceTestCase()
        {
        }

        public VsInstanceTestCase(string instanceId, IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments)
        {
            InstanceId = instanceId;
        }

        public string InstanceId { get; private set; }

        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            => new ExceptionRunner(this, new NotImplementedException(), string.Empty, messageBus, aggregator, cancellationTokenSource).RunAsync();

        public override void Deserialize(IXunitSerializationInfo info)
        {
            base.Deserialize(info);
            InstanceId = info.GetValue<string>(nameof(InstanceId));
        }

        public override void Serialize(IXunitSerializationInfo info)
        {
            base.Serialize(info);
            info.AddValue(nameof(InstanceId), InstanceId);
        }

        protected override void Initialize()
        {
            base.Initialize();
            Traits["Visual Studio"] = new List<string>(new[] { InstanceId });
        }

        protected override string GetDisplayName(IAttributeInfo factAttribute, string displayName)
            => $"Visual Studio [{InstanceId}]";

        protected override string GetUniqueID()
            => base.GetUniqueID() + "-" + InstanceId;
    }
}