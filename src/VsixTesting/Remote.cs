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

                if (Environment.Is64BitProcess)
                {
                    if (assemblyName.Name == "Microsoft.VisualStudio.Shell.11.0")
                    {
                        if (File.Exists("VsixTesting.Xunit.Tests.dll"))
                            return Assembly.Load("Microsoft.VisualStudio.Shell.15.0");
                    }
                }

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

        public static void InitServiceProviderGlobalProvider()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var majorVersion = Process.GetCurrentProcess().MainModule.FileVersionInfo.FileMajorPart;

                // Initialize ServiceProvider.GlobalProvider in Visual Studio 2010 SDK and above
                for (var shellVersion = majorVersion; shellVersion >= 10; shellVersion--)
                {
                    var type = Type.GetType($"Microsoft.VisualStudio.Shell.ServiceProvider, Microsoft.VisualStudio.Shell.{shellVersion}.0, Version={shellVersion}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false);
                    var prop = type?.GetProperty("GlobalProvider", new Type[0]);
                    prop?.GetValue(null);
                }

                // Initialize AsyncServiceProvider.GlobalProvider in Visual Studio 2015 SDK and above
                for (var shellVersion = majorVersion; shellVersion >= 14; shellVersion--)
                {
                    var type = Type.GetType($"Microsoft.VisualStudio.Shell.AsyncServiceProvider, Microsoft.VisualStudio.Shell.{shellVersion}.0, Version={shellVersion}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false);
                    var prop = type?.GetProperty("GlobalProvider", new Type[0]);
                    prop?.GetValue(null);
                }
            });
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