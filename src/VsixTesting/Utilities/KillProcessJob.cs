// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.Utilities
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;
    using static Interop.Kernel32;

    internal sealed class KillProcessJob
    {
        private SafeFileHandle jobObject;

        public KillProcessJob(params Process[] processes)
        {
            jobObject = CreateJobObject(IntPtr.Zero, null);
            if (jobObject.IsInvalid)
                throw new InvalidOperationException("Failed to create job object");

            var jobObjectELI = default(JOBOBJECT_EXTENDED_LIMIT_INFORMATION);
            jobObjectELI.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_FLAGS.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

            if (!SetInformationJobObject(jobObject, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, ref jobObjectELI, Marshal.SizeOf(jobObjectELI)))
                throw new InvalidOperationException("Failed to set job object information");

            foreach (var process in processes)
            {
                if (!AssignProcessToJobObject(jobObject, process.Handle))
                    throw new InvalidOperationException($"Failed to assign process {process.ProcessName} to job object");
            }
        }

        public void Release()
        {
            if (!jobObject.IsClosed)
            {
                var jobObjectELI = default(JOBOBJECT_EXTENDED_LIMIT_INFORMATION);
                jobObjectELI.BasicLimitInformation.LimitFlags = 0;
                SetInformationJobObject(jobObject, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, ref jobObjectELI, Marshal.SizeOf(jobObjectELI));
                jobObject.Dispose();
            }
        }
    }
}
