// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Common
{
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class ProcessExtensions
    {
        internal static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) => taskCompletionSource.TrySetResult(default);
            if (cancellationToken != default)
                cancellationToken.Register(() => taskCompletionSource.TrySetCanceled());

            return taskCompletionSource.Task;
        }
    }
}
