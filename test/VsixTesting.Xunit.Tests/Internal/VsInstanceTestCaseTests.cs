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
            var exceptionTestCase = new VsInstanceTestCase("VS 2015", "c:/path/to/devenv.exe", "Exp", new NullMessageSink(), default, default);
            exceptionTestCase.MergeSettings(new VsTestSettings { DebugMixedMode = true, ExtensionsDirectory = "dir" });
            exceptionTestCase.Serialize(info);

            var deserializedTestCase = new VsInstanceTestCase();
            deserializedTestCase.Deserialize(info);

            Assert.Equal(exceptionTestCase.InstanceId, deserializedTestCase.InstanceId);
            Assert.Equal(exceptionTestCase.ApplicationPath, deserializedTestCase.ApplicationPath);
            Assert.Equal(exceptionTestCase.RootSuffix, deserializedTestCase.RootSuffix);
            Assert.Equal(exceptionTestCase.DebugMixedMode, deserializedTestCase.DebugMixedMode);
            Assert.Equal(exceptionTestCase.ExtensionDirectories, deserializedTestCase.ExtensionDirectories);
        }
    }
}
