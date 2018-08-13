// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

#pragma warning disable CS0618 // Type or member is obsolete

namespace VsixTesting.XunitX.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using VsixTesting.XunitX.Internal.Utilities;
    using VsixTesting.XunitX.Tests.Utilities;
    using Xunit;
    using Xunit.Abstractions;
    using Xunit.Internal;
    using Xunit.Sdk;

    public class ExceptionTestCaseTests
    {
        [Fact]
        void SerializationWorks()
        {
            var exceptionTestCase = CreateTestCase();

            var info = new XunitSerializationInfo();
            exceptionTestCase.Serialize(info);

            var deserializedTestCase = new ExceptionTestCase();
            deserializedTestCase.Deserialize(info);

            Assert.Equal(exceptionTestCase.Types, deserializedTestCase.Types);
            Assert.Equal(exceptionTestCase.Messages, deserializedTestCase.Messages);
            Assert.Equal(exceptionTestCase.StackTraces, deserializedTestCase.StackTraces);
            Assert.Equal(exceptionTestCase.ParentIndices, deserializedTestCase.ParentIndices);
        }

        [Fact]
        async void TestRunnerWorks()
        {
            var bus = new MessageCollector();
            await CreateTestCase().RunAsync(new NullMessageSink(), bus, new string[] { }, new ExceptionAggregator(), new CancellationTokenSource());

            var testFailed = bus.Messages.OfType<ITestFailed>().FirstOrDefault();

            Assert.NotNull(testFailed);
            Assert.Equal(testFailed.ExceptionTypes, testFailed.ExceptionTypes);
            Assert.Equal(testFailed.Messages, testFailed.Messages);
            Assert.Equal(testFailed.StackTraces, testFailed.StackTraces);
            Assert.Equal(testFailed.ExceptionParentIndices, testFailed.ExceptionParentIndices);
        }

        static ExceptionTestCase CreateTestCase()
        {
            try
            {
                throw new Exception("Hello");
            }
            catch (Exception e)
            {
                var testMethod = Util.CreateTestMethod(typeof(ExceptionTestCaseTests), nameof(SerializationWorks));
                var exceptionTestCase = new ExceptionTestCase(new Exception("World", e), new NullMessageSink(), default, testMethod);
                return exceptionTestCase;
            }
        }

        private class MessageCollector : IMessageBus
        {
            public ConcurrentBag<IMessageSinkMessage> Messages { get; } = new ConcurrentBag<IMessageSinkMessage>();

            public void Dispose()
            {
            }

            public bool QueueMessage(IMessageSinkMessage message)
            {
                Messages.Add(message);
                return true;
            }
        }
    }
}
