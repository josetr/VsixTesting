// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Xunit
{
    using System;
    using VsixTesting.XunitX;
    using Xunit.Sdk;

    /// <summary>
    /// Attribute used to specify that a method is a theory that must be run in a Visual Studio process.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("VsixTesting.XunitX." + nameof(VsTheoryDiscoverer), ThisAssembly.AssemblyName)]
    public sealed class VsTheoryAttribute : VsFactAttribute
    {
    }
}