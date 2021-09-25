// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Common
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VsixTesting.Interop;

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

        internal static string GetMainModuleFileName(this Process process, int bufferCapacity = 2056)
        {
            try
            {
                if (process == null)
                    return string.Empty;
                var processPath = new StringBuilder(bufferCapacity);
                var result = Psapi.GetModuleFileNameEx(process.Handle, IntPtr.Zero, processPath, processPath.Capacity);
                if (result <= 0)
                    return string.Empty;
                if (result == processPath.Capacity - 1)
                    return GetMainModuleFileName(process, bufferCapacity * 2);
                return processPath.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        internal static string GetProcessName(this Process process)
        {
            return Path.GetFileNameWithoutExtension(GetMainModuleFileName(process));
        }
    }
}
