// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Channels;
    using System.Windows;
    using System.Windows.Threading;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using VsixTesting.Remoting;

    internal sealed class Remote
    {
        private static ResolveEventHandler resolver;
        private static IChannel channel;

        public static void SetAssemblyResolver(string directory, bool requireExactVersion)
        {
            resolver = (object sender, ResolveEventArgs eventArgs) =>
            {
                var assemblyName = new AssemblyName(eventArgs.Name);

                var assemblyFile = Path.Combine(directory, $"{assemblyName.Name}.dll");
                if (File.Exists(assemblyFile))
                {
                    var assembly = Assembly.LoadFrom(assemblyFile);

                    if (requireExactVersion == false || assembly.GetName().Version == assemblyName.Version)
                        return assembly;
                }

                return null;
            };

            AppDomain.CurrentDomain.AssemblyResolve += resolver;
        }

        public static void RegisterIpcChannel(string name, string portName, bool ensureSecurity)
            => channel = ChannelUtil.RegisterIpcChannel(name, portName, ensureSecurity);

        public static void RegisterWellKnownServiceType(string assemblyName, string fullTypeName, WellKnownObjectMode mode)
            => RemotingConfiguration.RegisterWellKnownServiceType(Assembly.Load(assemblyName).GetType(fullTypeName), fullTypeName, mode);

        public static void AutoKillWhenProcessExits(int processId)
        {
            var process = Process.GetProcessById(processId);
            process.EnableRaisingEvents = true;
            process.Exited += (_, e) => Process.GetCurrentProcess().Kill();
        }

        public static void InitVsTaskLibraryHelperServiceInstance()
        {
            Application.Current.Dispatcher.Invoke(
            () =>
            {
                var dte = ServiceProvider.GlobalProvider.GetService(typeof(SDTE)) as EnvDTE.DTE;
                for (var shellVersion = new Version(dte.Version).Major; shellVersion >= 14; shellVersion--)
                {
                    var type = Type.GetType($"Microsoft.VisualStudio.Shell.VsTaskLibraryHelper, Microsoft.VisualStudio.Shell.{shellVersion}.0, Version={shellVersion}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false);
                    var prop = type?.GetProperty("ServiceInstance", new Type[0]);
                    prop?.GetValue(null);
                }
            }, DispatcherPriority.Background);
        }

        public static void Dispose()
        {
            if (channel != null)
            {
                ChannelServices.UnregisterChannel(channel);
                channel = null;
            }

            if (resolver != null)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= resolver;
                resolver = null;
            }
        }
    }
}