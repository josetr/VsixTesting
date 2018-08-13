// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.Utilities
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using static Interop.Ole32;

    internal sealed class RetryMessageFilter : IMessageFilter, IDisposable
    {
        private const int RetryImmediately = 99;
        private const int CancelCall = -1;
        private readonly IMessageFilter prevFilter;
        private TimeSpan timeout = TimeSpan.FromSeconds(30);

        public RetryMessageFilter()
        {
            Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "This class requires a STA thread.");
            if (CoRegisterMessageFilter(this, out prevFilter) != S_OK)
                throw new InvalidOperationException("Cannot register ole message filter.");
        }

        ~RetryMessageFilter()
            => Dispose(false);

        int IMessageFilter.HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo)
            => (int)SERVERCALL.SERVERCALL_ISHANDLED;

        int IMessageFilter.RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType)
        {
            if (dwRejectType == (int)SERVERCALL.SERVERCALL_RETRYLATER && dwTickCount < timeout.TotalMilliseconds)
                return RetryImmediately;

            return CancelCall;
        }

        int IMessageFilter.MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType)
            => (int)PENDINGMSG.PENDINGMSG_WAITDEFPROCESS;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
            => CoRegisterMessageFilter(prevFilter, out _);
    }
}
