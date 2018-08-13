// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.Remoting
{
    using System;
    using System.Runtime.InteropServices;

    [ComImport]
    [Guid("00020400-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface IRemoteComInvoker
    {
        object InvokeMethod(string assemblyPath, string @class, string method, object obj, params object[] arguments);
    }
}