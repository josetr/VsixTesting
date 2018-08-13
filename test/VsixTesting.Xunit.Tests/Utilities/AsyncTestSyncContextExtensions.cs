// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Tests.Utilities
{
    using System.Reflection;
    using System.Threading;
    using Xunit.Sdk;

    static class AsyncTestSyncContextExtensions
    {
        public static SynchronizationContext GetInnerSyncContext(this AsyncTestSyncContext context)
        {
            return (SynchronizationContext)context.GetType()
                .GetField("innerContext", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(context);
        }
    }
}
