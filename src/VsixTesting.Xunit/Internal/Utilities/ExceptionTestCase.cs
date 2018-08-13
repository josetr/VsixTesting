// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Internal.Utilities
{
    using System;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class ExceptionTestCase : XunitTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public ExceptionTestCase()
        {
        }

        public ExceptionTestCase(Exception exception, IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod)
        {
            var finfo = ExceptionUtility.ConvertExceptionToFailureInformation(exception);
            Types = finfo.ExceptionTypes;
            Messages = finfo.Messages;
            StackTraces = finfo.StackTraces;
            ParentIndices = finfo.ExceptionParentIndices;
        }

        internal string[] Types { get; private set; }
        internal string[] Messages { get; private set; }
        internal string[] StackTraces { get; private set; }
        internal int[] ParentIndices { get; private set; }

        public override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);
            data.AddValue("ExceptionTypes", Types);
            data.AddValue("ExceptionMessages", Messages);
            data.AddValue("ExceptionStackTraces", StackTraces);
            data.AddValue("ExceptionParentIndices", ParentIndices);
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            base.Deserialize(data);
            Types = data.GetValue<string[]>("ExceptionTypes");
            Messages = data.GetValue<string[]>("ExceptionMessages");
            StackTraces = data.GetValue<string[]>("ExceptionStackTraces");
            ParentIndices = data.GetValue<int[]>("ExceptionParentIndices");
        }

        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            return new ExceptionRunner(this, Types, Messages, StackTraces, ParentIndices, string.Empty, messageBus, aggregator, cancellationTokenSource).RunAsync();
        }
    }
}