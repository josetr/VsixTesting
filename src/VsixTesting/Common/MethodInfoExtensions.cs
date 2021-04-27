// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Common
{
    using System.Reflection;
    using System.Runtime.CompilerServices;

    internal static class MethodInfoExtensions
    {
        public static bool IsAsync(this MethodInfo methodInfo)
        {
            return methodInfo.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;
        }
    }
}
