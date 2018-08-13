// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class VsTestRunner : XunitTestRunner
    {
        public VsTestRunner(ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, string skipReason, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
        }

        protected new VsTestCase TestCase => (VsTestCase)base.TestCase;

        protected override Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
        {
            var result = TestCase.Settings.UIThread
               ? InvokeOnUIThreadAsync(aggregator)
               : InvokeAsync(aggregator);

            return result;
        }

        private Task<decimal> InvokeOnUIThreadAsync(ExceptionAggregator aggregator)
        {
            var tcs = new TaskCompletionSource<decimal>();

            Application.Current.Dispatcher.BeginInvoke(
               new Action(async () =>
               {
                   try
                   {
                       var result = await InvokeAsync(aggregator);
                       tcs.SetResult(result);
                   }
                   catch (Exception e)
                   {
                       tcs.SetException(e);
                   }
               }), DispatcherPriority.Background);

            return tcs.Task;
        }

        private Task<decimal> InvokeAsync(ExceptionAggregator aggregator)
            => new VsTestInvoker(Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource).RunAsync();
    }
}
