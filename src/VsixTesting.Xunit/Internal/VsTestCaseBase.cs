// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Internal
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Xunit.Abstractions;
    using Xunit.Internal;
    using Xunit.Sdk;

    internal class VsTestCaseBase : XunitTestCase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public VsTestCaseBase()
        {
        }

        public VsTestCaseBase(string instanceId, IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments)
        {
            InstanceId = instanceId;
        }

        public string InstanceId { get; private set; }

        public override void Serialize(IXunitSerializationInfo info)
        {
            base.Serialize(info);
            info.AddValue(nameof(InstanceId), InstanceId);
        }

        public override void Deserialize(IXunitSerializationInfo info)
        {
            base.Deserialize(info);
            InstanceId = info.GetValue<string>(nameof(InstanceId));
        }

        public string SerializeToString()
        {
            var triple = new XunitSerializationTriple(nameof(VsTestCaseBase), this, GetType());
            return XunitSerializationInfo.SerializeTriple(triple);
        }

        public static VsTestCaseBase DeserializeFromString(string value)
        {
            var triple = XunitSerializationInfo.DeserializeTriple(value);
            return (VsTestCaseBase)triple.Value;
        }

        protected override string GetDisplayName(IAttributeInfo factAttribute, string displayName)
            => base.GetDisplayName(factAttribute, displayName) + " - " + InstanceId;

        protected override string GetUniqueID()
            => base.GetUniqueID() + "-" + InstanceId;

        protected override void Initialize()
        {
            base.Initialize();
            Traits["Visual Studio"] = new List<string>(new[] { InstanceId });
        }
    }
}
