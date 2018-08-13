// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace VsixTesting.Interop
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;

    internal class Kernel32
    {
        public enum JOBOBJECTINFOCLASS
        {
            JobObjectBasicLimitInformation = 2,
            JobObjectBasicUIRestrictions = 4,
            JobObjectSecurityLimitInformation = 5,
            JobObjectEndOfJobTimeInformation = 6,
            JobObjectAssociateCompletionPortInformation = 7,
            JobObjectExtendedLimitInformation = 9,
            JobObjectGroupInformation = 11,
        }

        public enum JOB_OBJECT_LIMIT_FLAGS
        {
            JOB_OBJECT_LIMIT_WORKINGSET = 1,
            JOB_OBJECT_LIMIT_PROCESS_TIME = 2,
            JOB_OBJECT_LIMIT_JOB_TIME = 4,
            JOB_OBJECT_LIMIT_ACTIVE_PROCESS = 8,
            JOB_OBJECT_LIMIT_AFFINITY = 16,
            JOB_OBJECT_LIMIT_PRIORITY_CLASS = 32,
            JOB_OBJECT_LIMIT_PRESERVE_JOB_TIME = 64,
            JOB_OBJECT_LIMIT_SCHEDULING_CLASS = 128,
            JOB_OBJECT_LIMIT_PROCESS_MEMORY = 256,
            JOB_OBJECT_LIMIT_JOB_MEMORY = 512,
            JOB_OBJECT_LIMIT_DIE_ON_UNHANDLED_EXCEPTION = 1024,
            JOB_OBJECT_LIMIT_BREAKAWAY_OK = 2048,
            JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK = 4096,
            JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 8192,
            JOB_OBJECT_LIMIT_SUBSET_AFFINITY = 16384,
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetInformationJobObject(
            SafeFileHandle hJob,
            JOBOBJECTINFOCLASS JobObjectInfoClass,
            ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInfo,
            int cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AssignProcessToJobObject(SafeFileHandle hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeFileHandle CreateJobObject(IntPtr lpJobAttributes, string lpName);

        public struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public JOB_OBJECT_LIMIT_FLAGS LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }
    }
}
