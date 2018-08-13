// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

#pragma warning disable CS0618 // Type or member is obsolete

namespace VsixTesting.XunitX.Tests
{
    using VsixTesting.XunitX.Internal;
    using Xunit;
    using Xunit.Internal;
    using Xunit.Sdk;

    public class VsInstanceTestCaseTests
    {
        [Fact]
        void SerializationWorks()
        {
            var info = new XunitSerializationInfo();
            var exceptionTestCase = new VsInstanceTestCase("VS 2015", new NullMessageSink(), default, default);
            exceptionTestCase.Serialize(info);

            var deserializedTestCase = new VsInstanceTestCase();
            deserializedTestCase.Deserialize(info);

            Assert.Equal(exceptionTestCase.InstanceId, deserializedTestCase.InstanceId);
        }
    }
}
