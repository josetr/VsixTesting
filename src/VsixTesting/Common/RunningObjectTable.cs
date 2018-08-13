// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.
#pragma warning disable SA1310 // Field names must not contain underscore

namespace Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using System.Text.RegularExpressions;
    using VsixTesting.Interop;

    internal static class RunningObjectTable
    {
        private const int S_OK = 0;

        public static IEnumerable<object> GetRunningObjects(string namePattern = ".*")
        {
            IEnumMoniker enumMoniker = null;
            IRunningObjectTable rot = null;
            IBindCtx bindCtx = null;

            try
            {
                Marshal.ThrowExceptionForHR(Ole32.CreateBindCtx(0, out bindCtx));
                bindCtx.GetRunningObjectTable(out rot);
                rot.EnumRunning(out enumMoniker);

                var moniker = new IMoniker[1];
                var fetched = IntPtr.Zero;

                while (enumMoniker.Next(1, moniker, fetched) == S_OK)
                {
                    object runningObject = null;
                    try
                    {
                        var roMoniker = moniker.First();
                        if (roMoniker == null)
                            continue;
                        roMoniker.GetDisplayName(bindCtx, null, out var name);
                        if (!Regex.IsMatch(name, namePattern))
                            continue;
                        Marshal.ThrowExceptionForHR(rot.GetObject(roMoniker, out runningObject));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        continue;
                    }

                    if (runningObject != null)
                        yield return runningObject;
                }
            }
            finally
            {
                if (enumMoniker != null)
                    Marshal.ReleaseComObject(enumMoniker);
                if (rot != null)
                    Marshal.ReleaseComObject(rot);
                if (bindCtx != null)
                    Marshal.ReleaseComObject(bindCtx);
            }
        }
    }
}
