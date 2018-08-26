// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace VsixTesting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Channels;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Vs;
    using VsixTesting.Remoting;
    using VsixTesting.Utilities;
    using DTE = EnvDTE.DTE;

    internal sealed class VsInstance
    {
        private string remoteChannelPortName;
        private IChannel channel;

        private VsInstance(Version version, Process process, DTE dte, IRemoteComInvoker invoker)
        {
            this.Version = version;
            this.Process = process;
            Dte = dte;
            ComInvoker = invoker;
        }

        public Version Version { get; }
        public Process Process { get; }
        public DTE Dte { get; }
        public IRemoteComInvoker ComInvoker { get; }

        public static async Task Prepare(VsHive hive, IEnumerable<string> extensionsToInstall, bool resetSettings, IDiagnostics diagnostics)
        {
            await diagnostics.RunAsync("Preparing Instance", async output =>
            {
                var installResult = default((int InstallCount, bool HasSettingsFile, string Output));
                var invokerAssembly = Assembly.GetExecutingAssembly();

                using (var invoker = new TempFile(EmbeddedResourceUtil.ExtractResource(invokerAssembly, "VsixTesting.Invoker.vsix")))
                {
                    EmbeddedResourceUtil.ApplyDateTime(invoker.Path, invokerAssembly, "VsixTesting.Invoker.vsix");

                    installResult = await VisualStudioUtil.InstallExtensionsAsync(
                        hive,
                        extensionsToInstall.Concat(new[] { invoker.Path }));

                    output.WriteLine(installResult.Output);

                    if (installResult.InstallCount > 0)
                    {
                        output.WriteLine("Clearing cache");
                        await VisualStudioUtil.ClearCacheAsync(hive);
                        output.WriteLine("Updating configuration");
                        await VisualStudioUtil.UpdateConfigurationAsync(hive);
                    }
                }

                if (!installResult.HasSettingsFile || resetSettings)
                {
                    output.WriteLine("Resetting settings");
                    await VisualStudioUtil.ResetSettingsAsync(hive);
                }
            });
        }

        public static Task<VsInstance> Launch(VsHive hive, TimeSpan timeout, IDiagnostics diagnostics)
        {
            Process process = null;
            KillProcessJob killProcessJob = null;
            return diagnostics.RunAsync("Launching Instance", async output =>
            {
                try
                {
                    process = VisualStudioUtil.StartProcess(hive);
                    killProcessJob = new KillProcessJob(process);
                    var dte = await VisualStudioUtil.GetDTE(process, timeout);
                    var invoker = (IRemoteComInvoker)dte.GetObject("VsixTesting.Invoker");
                    InvokeRemote(invoker, nameof(Remote.InitVsTaskLibraryHelperServiceInstance));
                    InvokeRemote(invoker, nameof(Remote.AutoKillWhenProcessExits), Process.GetCurrentProcess().Id);
                    killProcessJob.Release();
                    return new VsInstance(hive.Version, process, dte, invoker);
                }
                catch
                {
                    process?.Kill();
                    process?.Dispose();
                    throw;
                }
            });
        }

        public static Task AttachDebugger(Process process, bool debugMixedMode, IDiagnostics diagnostics)
        {
            return diagnostics.RunAsync("Attaching Debugger", output =>
            {
                var dte = VisualStudioUtil.GetDteFromDebuggedProcess(Process.GetCurrentProcess());
                if (dte != null)
                    VisualStudioUtil.AttachDebugger(dte, process, debugMixedMode ? "Managed/Native" : "Managed");
            });
        }

        public void SetAssemblyResolver(string directory, bool requireExactVersion = false)
            => InvokeRemote(nameof(Remote.SetAssemblyResolver), directory, requireExactVersion);

        public T GetOrCreateSingletonService<T>(string assemblyName, bool ensureSecurity = false)
        {
            if (channel == null)
            {
                channel = ChannelUtil.RegisterIpcChannel("VsixTesting.ClientChannel", Guid.NewGuid().ToString(), ensureSecurity);

                remoteChannelPortName = Guid.NewGuid().ToString();
                InvokeRemote(nameof(Remote.RegisterIpcChannel), "VsixTesting.ServerChannel", remoteChannelPortName, ensureSecurity);
                InvokeRemote(nameof(Remote.RegisterWellKnownServiceType), assemblyName, typeof(T).FullName, WellKnownObjectMode.Singleton);
            }

            return (T)RemotingServices.Connect(typeof(T), $"ipc://{remoteChannelPortName}/{typeof(T).FullName}");
        }

        public async Task DisposeAsync()
        {
            if (channel != null)
            {
                ChannelServices.UnregisterChannel(channel);
                channel = null;
            }

            if (Process != null && !Process.HasExited)
            {
                Process.CloseMainWindow();
                try
                {
                    await Process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
                }
                catch (TaskCanceledException)
                {
                    if (!Process.HasExited)
                        Process.Kill();
                }

                Process.Dispose();
            }
        }

        private object InvokeRemote(string method, params object[] arguments)
            => InvokeRemote(ComInvoker, method, arguments);

        private static object InvokeRemote(IRemoteComInvoker comInvoker, string method, params object[] arguments)
        {
            var assemblyPath = new Uri(typeof(Remote).Assembly.CodeBase).AbsolutePath;
            return comInvoker.InvokeMethod(assemblyPath, typeof(Remote).FullName, method, null, arguments);
        }
    }
}