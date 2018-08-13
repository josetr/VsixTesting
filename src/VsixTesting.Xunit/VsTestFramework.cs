// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Xunit
{
    using System;
    using System.Reflection;
    using VsixTesting.XunitX.Internal;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class VsTestFramework : XunitTestFramework
    {
        private static bool isUsed = false;

        public VsTestFramework(IMessageSink messageSink)
            : base(messageSink)
        {
            isUsed = true;
        }

        internal static void ThrowIfNotInUse()
        {
            if (isUsed == false)
            {
                var type = MethodBase.GetCurrentMethod().DeclaringType;
                var assemblyName = type.Assembly.GetName().Name;
                var attribute = $"TestFramework(\"{type.FullName}\", \"{assemblyName}\")]";
                throw new InvalidOperationException($"[assembly: {attribute} is required.");
            }
        }

        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        {
            return new VsTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
        }
    }
}
