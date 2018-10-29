// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Vs
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Common;
    using EnvDTE80;
    using DTE = EnvDTE.DTE;

    internal static partial class VisualStudioUtil
    {
        public static DTE GetDTE(Process process)
        {
            string namePattern = $@"!VisualStudio\.DTE\.(\d+\.\d+):{process.Id.ToString()}";
            return (DTE)RunningObjectTable.GetRunningObjects(namePattern).FirstOrDefault();
        }

        public static async Task<DTE> GetDTE(Process process, TimeSpan timeout)
        {
            var msTimeout = timeout.TotalMilliseconds;
            while (true)
            {
                var dte = GetDTE(process);
                if (dte != null)
                    return dte;
                await Task.Delay(250);
                if ((msTimeout -= 250) <= 0)
                    throw new TimeoutException($"Failed getting DTE from {process.ProcessName} after waiting {timeout.TotalSeconds} seconds");
            }
        }

        public static IEnumerable<DTE> GetRunningDTEs()
        {
            foreach (var process in Process.GetProcessesByName("devenv"))
            {
                if (GetDTE(process) is DTE dte)
                    yield return dte;
            }
        }

        public static DTE GetDteFromDebuggedProcess(Process process)
        {
            foreach (var dte in GetRunningDTEs())
            {
                try
                {
                    if (dte.Debugger.CurrentMode != EnvDTE.dbgDebugMode.dbgDesignMode)
                    {
                        foreach (Process2 debuggedProcess in dte.Debugger.DebuggedProcesses)
                        {
                            if (debuggedProcess.ProcessID == process.Id)
                                return dte;
                        }
                    }
                }
                catch (COMException)
                {
                    continue;
                }
            }

            return null;
        }

        public static void AttachDebugger(DTE dte, Process targetProcess, string engine = "Managed")
        {
            dte?.Debugger
                .LocalProcesses
                .Cast<Process2>()
                .FirstOrDefault(p => p.ProcessID == targetProcess.Id)
                ?.Attach2(engine);
        }
    }
}
