// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

#pragma warning disable CS0618 // Type or member is obsolete

namespace VsixTesting.XunitX.Tests
{
    using VsixTesting.XunitX.Internal;
    using VsixTesting.XunitX.Tests.Utilities;
    using Xunit;
    using Xunit.Internal;
    using Xunit.Sdk;

    public class VsSkippedDataRowTestCaseTests
    {
        [Fact]
        void SerializationWorks()
        {
            var info = new XunitSerializationInfo();
            var testMethod = Util.CreateTestMethod(typeof(VsSkippedDataRowTestCaseTests), nameof(SerializationWorks));

            var exceptionTestCase = new VsSkippedDataRowTestCase("VS 2015", default, default, testMethod, "No Reason");
            exceptionTestCase.Serialize(info);

            var deserializedTestCase = new VsSkippedDataRowTestCase();
            deserializedTestCase.Deserialize(info);

            Assert.Equal(exceptionTestCase.InstanceId, deserializedTestCase.InstanceId);
            Assert.Equal(deserializedTestCase.SkipReason, deserializedTestCase.SkipReason);
        }
    }
}
