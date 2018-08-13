// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Internal
{
    using System;
    using System.ComponentModel;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class VsSkippedDataRowTestCase : VsTestCaseBase
    {
        private string skipReason;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public VsSkippedDataRowTestCase()
        {
        }

        public VsSkippedDataRowTestCase(string instanceId, IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, string skipReason, object[] testMethodArguments = null)
            : base(instanceId, diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments)
        {
            this.skipReason = skipReason;
        }

        public override void Serialize(IXunitSerializationInfo info)
        {
            base.Serialize(info);
            info.AddValue("SkipReason", skipReason);
        }

        public override void Deserialize(IXunitSerializationInfo info)
        {
            base.Deserialize(info);
            skipReason = info.GetValue<string>("SkipReason");
        }

        protected override string GetSkipReason(IAttributeInfo factAttribute)
            => skipReason;
    }
}
