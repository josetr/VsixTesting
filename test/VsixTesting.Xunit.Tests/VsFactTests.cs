﻿// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Windows.Threading;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Vs;
    using VsixTesting.XunitX.Internal.Utilities;
    using VsixTesting.XunitX.Tests.Utilities;
    using Xunit;
    using Xunit.Abstractions;
    using Xunit.Sdk;
    using Task = System.Threading.Tasks.Task;

    public class VsFactTests
    {
        [VsFact]
        void FactWorks()
            => Assert.True(true);

        [VsFact]
        void FactRunningInsideVisualStudio()
            => Assert.NotNull(VisualStudioUtil.GetDTE(Process.GetCurrentProcess()));

        [VsFact(Skip = "Fact Skip works.")]
        void FactSkipWorks()
            => throw new NotImplementedException();

        [VsFact]
        void WebBrowsingServiceIsAvailable()
            => Assert.NotNull(Package.GetGlobalService(typeof(SVsWebBrowsingService)));

        public class ScreenShotTests : IDisposable
        {
            private const string ScreenshotsDirectory = "Screenshots";

            public ScreenShotTests()
            {
                if (Directory.Exists(ScreenshotsDirectory))
                    Directory.Delete(ScreenshotsDirectory, true);
            }

            [VsFact(TakeScreenshotOnFailure = true)]
            void DirectoryNotEmpty()
            {
                throw new FakeException();
            }

            public void Dispose()
            {
                Assert.NotEmpty(Directory.GetFiles(ScreenshotsDirectory));
                Directory.Delete(ScreenshotsDirectory, true);
            }
        }

        [VsTestSettings(UIThread = true)]
        public class UIThreadTests
        {
            readonly SynchronizationContext synchronizationContext;
            readonly int constructorThreadId;
            private ITestOutputHelper h;

            public UIThreadTests(ITestOutputHelper h)
            {
                this.h = h;
                Assert.IsType<DispatcherSynchronizationContext>(SynchronizationContext.Current);
                Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
                constructorThreadId = Thread.CurrentThread.ManagedThreadId;
                synchronizationContext = SynchronizationContext.Current;
            }

            [VsFact]
            void MethodHasSameManagedThreadAsConstructor()
                => Assert.Equal(constructorThreadId, Thread.CurrentThread.ManagedThreadId);

            [VsFact]
            void MethodHasSameSyncContextTypeAsConstructor()
            {
                var asyncTestSyncContext = (AsyncTestSyncContext)SynchronizationContext.Current;
                Assert.IsType(synchronizationContext.GetType(), asyncTestSyncContext.GetInnerSyncContext());
            }

            [VsFact]
            async void ContinuationRunsOnSameManagedThread()
            {
                var currentThread = Thread.CurrentThread.ManagedThreadId;
                Assert.Equal(currentThread, Thread.CurrentThread.ManagedThreadId);
                await Task.Yield();
                Assert.Equal(currentThread, Thread.CurrentThread.ManagedThreadId);
            }

            [VsFact]
            async void ContinuationRunsOnSameStaThread()
            {
                Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
                await Task.Yield();
                Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
            }

            [VsFact]
            async void ContinuationRunsOnSameSynchronizationContext()
            {
                var asyncTestSyncContext = (AsyncTestSyncContext)SynchronizationContext.Current;
                Assert.IsType<DispatcherSynchronizationContext>(asyncTestSyncContext.GetInnerSyncContext());
                await Task.Yield();
                Assert.IsType<DispatcherSynchronizationContext>(SynchronizationContext.Current);
            }

            [VsFact(Version = "2017")]
            public void CanFindVsTaskLibraryHelperServiceInstance()
            {
                // Check VsTaskLibraryHelper.ServiceInstance from Microsoft.VisualStudio.Shell.14.0
                // has been initialized when running in Visual Studio 2017
                var serviceInstance = GetVsTaskLibraryHelperServiceInstance(14);
                Assert.NotNull(serviceInstance);
            }

            private static object GetVsTaskLibraryHelperServiceInstance(int version)
            {
                var type = Type.GetType($"Microsoft.VisualStudio.Shell.VsTaskLibraryHelper, Microsoft.VisualStudio.Shell.{version}.0, Version={version}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false);
                var prop = type?.GetProperty("ServiceInstance", new Type[0]);
                return prop?.GetValue(null);
            }
        }

        public class BackgroundThreadTests
        {
            [VsFact(UIThread = false)]
            async void BackgroundThread()
            {
                var asyncTestSyncContext = (AsyncTestSyncContext)SynchronizationContext.Current;
                Assert.IsType<MaxConcurrencySyncContext>(asyncTestSyncContext.GetInnerSyncContext());
                await Task.Yield();
                Assert.IsType<MaxConcurrencySyncContext>(SynchronizationContext.Current);
            }
        }
    }
}