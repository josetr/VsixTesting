// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.Utilities
{
    using System;
    using System.Diagnostics;
    using VsixTesting.Interop;

    internal struct ProcessUtil
    {
        public static Process TryGetParentProcess(Process process)
        {
            try
            {
                var processInformation = default(Ntdll.PROCESS_BASIC_INFORMATION);
                if (Ntdll.NtQueryInformationProcess(process.Handle, 0, ref processInformation, processInformation.Size, out var returnLength) == Ntdll.STATUS_SUCCESS)
                    return Process.GetProcessById(processInformation.InheritedFromUniqueProcessId.ToInt32());
            }
            catch
            {
            }

            return null;
        }

        public static Process TryGetParentProcess(Process process, Func<Process, bool> predicate)
        {
            while (true)
            {
                process = TryGetParentProcess(process);
                if (process == null)
                    return null;

                if (predicate(process))
                    return process;
            }
        }
    }
}