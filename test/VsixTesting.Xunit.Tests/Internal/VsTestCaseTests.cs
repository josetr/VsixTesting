// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

#pragma warning disable CS0618 // Type or member is obsolete

namespace VsixTesting.XunitX.Tests
{
    using VsixTesting.XunitX.Internal;
    using VsixTesting.XunitX.Tests.Utilities;
    using Xunit;
    using Xunit.Internal;

    public class VsTestCaseTests
    {
        [Fact]
        void SerializationWorks()
        {
            var testMethod = Util.CreateTestMethod(typeof(VsTestCaseTests), nameof(SerializationWorks));
            var info = new XunitSerializationInfo();

            var exceptionTestCase = new VsTestCase("VS 2015", "C:/path/to/instance.exe", default, default, default, testMethod);
            exceptionTestCase.Serialize(info);

            var deserializedTestCase = new VsTestCase();
            deserializedTestCase.Deserialize(info);

            Assert.Equal(exceptionTestCase.InstanceId, deserializedTestCase.InstanceId);
            Assert.Equal(deserializedTestCase.SkipReason, deserializedTestCase.SkipReason);
        }
    }
}
