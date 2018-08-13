// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1310 // Field names should not contain underscore

namespace VsixTesting.Interop
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;

    internal static class Ole32
    {
        public const int S_OK = 0;

        internal enum PENDINGMSG
        {
            PENDINGMSG_CANCELCALL,
            PENDINGMSG_WAITNOPROCESS,
            PENDINGMSG_WAITDEFPROCESS,
        }

        internal enum SERVERCALL
        {
            SERVERCALL_ISHANDLED,
            SERVERCALL_REJECTED,
            SERVERCALL_RETRYLATER,
        }

        [ComImport]
        [Guid("00000016-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMessageFilter
        {
            [PreserveSig]
            int HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo);
            [PreserveSig]
            int RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType);
            [PreserveSig]
            int MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType);
        }

        [DllImport("ole32.dll")]
        public static extern int CoRegisterMessageFilter(IMessageFilter lpMessageFilter, out IMessageFilter lplpMessageFilter);

        [DllImport("ole32.dll")]
        public static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);
    }
}