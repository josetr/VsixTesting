// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.
#pragma warning disable SA1310 // Field names should not contain underscore

namespace VsixTesting.Interop
{
    using System;
    using System.Runtime.InteropServices;

    internal class Ntdll
    {
        public const int STATUS_SUCCESS = 0;

        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

        public struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public UIntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
            public int Size => Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION));
        }
    }
}
