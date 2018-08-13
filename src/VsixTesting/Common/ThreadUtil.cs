// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class ThreadUtil
    {
        public static Task<T> RunOnStaThreadAsync<T>(Func<Task<T>> func)
        {
            var tcs = new TaskCompletionSource<T>();
            var thread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    using (var syncContext = new SingleThreadSyncContext())
                    {
                        SynchronizationContext.SetSynchronizationContext(syncContext);
                        var task = func();
                        task.ContinueWith(delegate { syncContext.CompleteAdding(); }, TaskScheduler.Default);
                        syncContext.Run();
                        tcs.SetResult(task.GetAwaiter().GetResult());
                    }
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        public static Task RunOnStaThreadAsync(Func<Task> func)
        {
            return RunOnStaThreadAsync(async () =>
            {
                await func();
                return Task.FromResult(0);
            });
        }

        private sealed class SingleThreadSyncContext : SynchronizationContext, IDisposable
        {
            private readonly BlockingCollection<(SendOrPostCallback, object)> statefulCallbacks = new BlockingCollection<(SendOrPostCallback, object)>();
            private bool disposed = false;

            public override void Post(SendOrPostCallback d, object state)
                => statefulCallbacks.Add((d, state));

            public override void Send(SendOrPostCallback d, object state)
                => throw new NotSupportedException();

            public void Run()
            {
                foreach (var statefulCallback in statefulCallbacks.GetConsumingEnumerable())
                    statefulCallback.Item1(statefulCallback.Item2);
            }

            public void CompleteAdding()
                => statefulCallbacks.CompleteAdding();

            public void Dispose()
            {
                if (!disposed)
                {
                    statefulCallbacks.Dispose();
                    disposed = true;
                }
            }
        }
    }
}
