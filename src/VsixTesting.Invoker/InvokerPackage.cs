// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting.Invoker
{
    using System;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using EnvDTE;

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideAutomationObject(AutomationObjectName)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)]
    public sealed class InvokerPackage : Package
    {
        private const string AutomationObjectName = "VsixTesting.Invoker";
        private Lazy<InvokerService> automationObject = new Lazy<InvokerService>(() => new InvokerService());

        protected sealed override object GetAutomationObject(string name)
        {
            if (name == AutomationObjectName)
                return automationObject.Value;
            return base.GetAutomationObject(name);
        }

        protected override void Initialize()
        {
            InitializeThreadHelper();
        }

        private void InitializeThreadHelper()
        {
            if (GetService(typeof(DTE)) is DTE dte)
            {
                // Initialize the VsTaskLibraryHelper.ServiceInstance property on UI thread for Microsoft.VisualStudio.Shell.14.0 and above
                var majorVersion = new Version(dte.Version).Major;
                for (var shellVersion = majorVersion; shellVersion >= 14; shellVersion--)
                {
                    GetVsTaskLibraryHelperServiceInstance(shellVersion);
                }
            }
        }

        private static object GetVsTaskLibraryHelperServiceInstance(int version)
        {
            var type = Type.GetType($"Microsoft.VisualStudio.Shell.VsTaskLibraryHelper, Microsoft.VisualStudio.Shell.{version}.0, Version={version}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false);
            var prop = type?.GetProperty("ServiceInstance", new Type[0]);
            return prop?.GetValue(null);
        }
    }
}
