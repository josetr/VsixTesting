// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Common.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class ThreadUtilTests
    {
        [Fact]
        public async Task RunOnStaThreadAsync()
        {
            Assert.Equal(1520, await ThreadUtil.RunOnStaThreadAsync(async () =>
            {
                Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
                var id = Thread.CurrentThread.ManagedThreadId;
                await Task.Yield();
                Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
                Assert.Equal(Thread.CurrentThread.ManagedThreadId, id);
                return 1520;
            }));
        }
    }
}
