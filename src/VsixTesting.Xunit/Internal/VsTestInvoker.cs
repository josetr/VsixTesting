// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using VsixTesting.Utilities;
    using VsixTesting.XunitX.Internal.Utilities;
    using Xunit.Abstractions;
    using Xunit.Sdk;
    using Common;

    internal class VsTestInvoker : XunitTestInvoker
    {
        public VsTestInvoker(ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
        }

        protected VsTestSettings Settings => ((VsTestCase)TestCase).Settings;

        protected override object CallTestMethod(object testClassInstance)
        {
            try
            {
                return TestMethod.Invoke(testClassInstance, TestMethodArguments);
            }
            catch (Exception e)
            {
                if (Settings.TakeScreenshotOnFailure)
                {
                    try
                    {
                        TakeScreenShot(Test.DisplayName);
                    }
                    catch (Exception exception)
                    {
                        Aggregator.Add(exception);
                    }
                }

                if (e is TargetInvocationException && e.InnerException is FakeException)
                    return null;

                throw;
            }
        }

        protected override Task<decimal> InvokeTestMethodAsync(object testClassInstance)
        {
            if (Settings.UIThread && !TestMethod.IsAsync())
            {
                ValidateTestMethodParameters();
                Timer.Aggregate(() => CallTestMethod(testClassInstance));
                return Task.FromResult(Timer.Total);
            }

            return base.InvokeTestMethodAsync(testClassInstance);
        }

        private void TakeScreenShot(string name)
        {
            try
            {
                var safename = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
                var date = DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss");
                var path = Path.Combine(Settings.ScreenshotsDirectory, $"{date} {safename}.png");

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                ScreenshotUtil.CaptureWindow(Process.GetCurrentProcess().MainWindowHandle, path);
            }
            catch (Exception e)
            {
                Aggregator.Add(new Exception("Failed saving screenshot", e));
            }
        }

        // https://github.com/xunit/xunit/src/xunit.execution/Sdk/Frameworks/Runners/TestInvoker.cs#L251
        private void ValidateTestMethodParameters()
        {
            int parameterCount = TestMethod.GetParameters().Length;
            int valueCount = (TestMethodArguments != null) ? TestMethodArguments.Length : 0;
            if (parameterCount != valueCount)
            {
                Aggregator.Add(new InvalidOperationException(string.Format(
                    "The test method expected {0} parameter value{1}, but {2} parameter value{3} {4} provided.", parameterCount, (parameterCount == 1) ? string.Empty : "s", valueCount, (valueCount == 1) ? string.Empty : "s", (valueCount == 1) ? "was" : "were")));
            }
        }
    }
}
