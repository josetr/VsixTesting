// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.XunitX.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Windows.Threading;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Vs;
    using VsixTesting.XunitX.Tests.Utilities;
    using Xunit;
    using Xunit.Sdk;
    using Task = System.Threading.Tasks.Task;

    public class VsTheoryTests
    {
        private static HashSet<int> theoryWorksNumbers = new HashSet<int>();

        [VsTheory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(5)]
        void TheoryWorks(int number)
        {
            Assert.True(theoryWorksNumbers.Add(number));
            Assert.True(number == 1 || number == 5 || number == 100);
        }

        [VsTheory]
        [InlineData(0)]
        void TheoryRunningInsideVisualStudio(int zero)
        {
            Assert.NotNull(VisualStudioUtil.GetDTEObject(Process.GetCurrentProcess()));
            Assert.Equal(0, zero);
        }

        [VsTheory]
        [MemberData(nameof(Data))]
        void TheoryWithNonSerializableClassDataWorks(ClassData c)
        {
            Assert.NotNull(Package.GetGlobalService(typeof(SVsWebBrowsingService)));
            Assert.True(c.Value == 1 || c.Value == 5 || c.Value == 100);
        }

        [VsTheory(Skip = "Theory Skip works.")]
        [InlineData(0)]
        [InlineData(1)]
        void TheorySkipWorks(int n)
            => throw new NotImplementedException();

        [VsTheory]
        [InlineData(0)]
        [InlineData(1, Skip = "Theory Data Row Skip works.")]
        void TheoryDataRowSkipWorks(int n)
            => Assert.Equal(0, n);

        public static IEnumerable<object[]> Data =>
            new List<object[]>
            {
                new object[] { new ClassData(1) },
                new object[] { new ClassData(100) },
                new object[] { new ClassData(5) },
            };

        [VsTheory]
        [InlineData(0)]
        void WebBrowsingServiceIsAvailable(int zero)
        {
            Assert.NotNull(Package.GetGlobalService(typeof(SVsWebBrowsingService)));
            Assert.Equal(0, zero);
        }

        [VsTestSettings(UIThread = true)]
        public class UIThreadTests
        {
            readonly SynchronizationContext synchronizationContext;
            readonly int constructorThreadId;

            public UIThreadTests()
            {
                Assert.IsType<DispatcherSynchronizationContext>(SynchronizationContext.Current);
                Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
                constructorThreadId = Thread.CurrentThread.ManagedThreadId;
                synchronizationContext = SynchronizationContext.Current;
            }

            [VsTheory]
            [InlineData(0)]
            void MethodHasSameManagedThreadAsConstructor(int zero)
                => Assert.Equal(constructorThreadId, Thread.CurrentThread.ManagedThreadId);

            [VsTheory]
            [InlineData(0)]
            void MethodHasSameSyncContextTypeAsConstructor(int zero)
            {
                var asyncTestSyncContext = (AsyncTestSyncContext)SynchronizationContext.Current;
                Assert.IsType(synchronizationContext.GetType(), asyncTestSyncContext.GetInnerSyncContext());
            }

            [VsTheory]
            [InlineData(0)]
            async void ContinuationRunsOnSameManagedThread(int zero)
            {
                var currentThread = Thread.CurrentThread.ManagedThreadId;
                Assert.Equal(currentThread, Thread.CurrentThread.ManagedThreadId);
                await Task.Yield();
                Assert.Equal(currentThread, Thread.CurrentThread.ManagedThreadId);
            }

            [VsTheory]
            [InlineData(0)]
            async void ContinuationRunsOnSameStaThread(int zero)
            {
                Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
                await Task.Yield();
                Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
            }

            [VsTheory]
            [InlineData(0)]
            async void ContinuationRunsOnSameSynchronizationContext(int zero)
            {
                var asyncTestSyncContext = (AsyncTestSyncContext)SynchronizationContext.Current;
                Assert.IsType<DispatcherSynchronizationContext>(asyncTestSyncContext.GetInnerSyncContext());
                await Task.Yield();
                Assert.IsType<DispatcherSynchronizationContext>(SynchronizationContext.Current);
            }
        }
    }

    public class ClassData
    {
        public int Value { get; }

        public ClassData(int v)
        {
            Value = v;
        }

        public override string ToString()
        {
            return this.Value.ToString();
        }
    }
}