// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Vs
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Common;
    using EnvDTE80;
    using Microsoft.VisualStudio.Setup.Configuration;
    using Microsoft.Win32;
    using VsixTesting.Utilities;
    using DTE = EnvDTE.DTE;

    internal static class VisualStudioUtil
    {
        public const string ProcessName = "devenv";

        public static DTE GetDTE(Process process)
        {
            string namePattern = $@"!VisualStudio\.DTE\.(\d+\.\d+):{process.Id.ToString()}";
            return (DTE)RunningObjectTable.GetRunningObjects(namePattern).FirstOrDefault();
        }

        public static async Task<DTE> GetDTE(Process process, TimeSpan timeout)
        {
            var msTimeout = timeout.TotalMilliseconds;
            while (true)
            {
                var dte = GetDTE(process);
                if (dte != null)
                    return dte;
                await Task.Delay(250);
                if ((msTimeout -= 250) <= 0)
                    throw new TimeoutException($"Failed getting DTE from {process.ProcessName} after waiting {timeout.TotalSeconds} seconds");
            }
        }

        public static IEnumerable<DTE> GetRunningDTEs()
        {
            foreach (var process in Process.GetProcessesByName(ProcessName))
            {
                if (GetDTE(process) is DTE dte)
                    yield return dte;
            }
        }

        public static DTE GetDteFromDebuggedProcess(Process process)
        {
            foreach (var dte in GetRunningDTEs())
            {
                try
                {
                    if (dte.Debugger.CurrentMode != EnvDTE.dbgDebugMode.dbgDesignMode)
                    {
                        foreach (Process2 debuggedProcess in dte.Debugger.DebuggedProcesses)
                        {
                            if (debuggedProcess.ProcessID == process.Id)
                                return dte;
                        }
                    }
                }
                catch (COMException)
                {
                    continue;
                }
            }

            return null;
        }

        public static void AttachDebugger(DTE dte, Process targetProcess, string engine = "Managed")
        {
            dte?.Debugger
                .LocalProcesses
                .Cast<Process2>()
                .FirstOrDefault(p => p.ProcessID == targetProcess.Id)
                ?.Attach2(engine);
        }

        public static IEnumerable<VsInstallation> FindInstallations()
        {
            using (var visualStudioRegKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio"))
            {
                if (visualStudioRegKey != null)
                {
                    foreach (string versionKey in visualStudioRegKey.GetSubKeyNames())
                    {
                        if (!Version.TryParse(versionKey, out var version))
                            continue;

                        using (var vs = visualStudioRegKey.OpenSubKey($@"{versionKey}\Setup\VS"))
                        using (var devenv = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\DevDiv\VS\Servicing\{version.Major}.0\devenv"))
                        {
                            var installationPath = vs?.GetValue("ProductDir") as string;
                            var devEnvVersion = devenv?.GetValue("Version") as string;
                            if (IsValidInstallationDirectory(installationPath))
                            {
                                yield return new VsInstallation(
                                    version: version,
                                    path: installationPath,
                                    name: $"Visual Studio/{devEnvVersion ?? version.ToString(2)}");
                            }
                        }
                    }
                }
            }

            var setupConfiguration = new SetupConfiguration();
            var setupInstanceEnumerator = setupConfiguration.EnumInstances();
            var setupInstances = new ISetupInstance2[1];

            while (true)
            {
                setupInstanceEnumerator.Next(setupInstances.Length, setupInstances, out var fetched);
                if (fetched == 0) break;
                var instance = setupInstances[0];

                if (IsValidInstallationDirectory(instance.GetInstallationPath()))
                {
                    yield return new VsInstallation(
                        version: new Version(instance.GetInstallationVersion()),
                        name: instance.GetInstallationName(),
                        path: instance.GetInstallationPath())
                    {
                        PackageIds = instance.GetPackages().Select(p => p.GetId()),
                    };
                }
            }
        }

        public static void ThrowOnBadProcessName(string processName)
        {
            if (processName.EndsWith("Express", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException($"Visual Studio Express is not supported.");

            if (!processName.Equals(ProcessName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"A process with a name '{ProcessName}' is required.", nameof(processName));
        }

        public static Process StartProcess(VsHive hive, string arguments = "")
            => Process.Start(hive.ApplicationPath, $"{GetRootSuffixAsArgument(hive.RootSuffix)} {arguments}");

        public static async Task ClearCacheAsync(VsHive hive)
        {
            if (hive.Version.Major >= (int)VsVersion.VS2013)
                await StartProcess(hive, "/clearcache").WaitForExitAsync();
        }

        public static async Task UpdateConfigurationAsync(VsHive hive)
            => await StartProcess(hive, "/updateconfiguration").WaitForExitAsync();

        public static async Task ResetSettingsAsync(VsHive hive, string settingsName = "General.vssettings")
            => await StartProcess(hive, $"/resetsettings {settingsName} /command \"File.Exit\"").WaitForExitAsync();

        public static async Task<(int InstallCount, bool HasSettingsFile, string Output)> InstallExtensionsAsync(VsHive hive, IEnumerable<string> extensions)
        {
            using (var visualStudioInstaller = new TempFile(EmbeddedResourceUtil.ExtractResource(Assembly.GetExecutingAssembly(), "VsixTesting.ExtensionInstaller.exe")))
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = visualStudioInstaller.Path,
                    Arguments = string.Join(" ", new string[]
                    {
                        "--ApplicationPath", Quote(hive.ApplicationPath),
                        "--RootSuffix", hive.RootSuffix,
                        "--ExtensionPaths",
                    }.Concat(extensions.Select(e => Quote(e)))),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                });

                await process.WaitForExitAsync();

                var result = process.ExitCode;
                var hasSettingsFile = true;

                if (result < 0)
                    throw new Exception(process.StandardError.ReadToEnd());
                else if (result >= 9999)
                {
                    result -= 9999;
                    hasSettingsFile = false;
                }

                return (result, hasSettingsFile, process.StandardOutput.ReadToEnd());
            }

            string Quote(string str) => $"\"{str}\"";
        }

        internal static bool IsValidInstallationDirectory(string installationPath)
            => !string.IsNullOrWhiteSpace(installationPath) && File.Exists(GetApplicationPath(installationPath));

        internal static string GetApplicationPath(string installationPath)
            => Path.Combine(installationPath, "Common7", "IDE", ProcessName + ".exe");

        private static string GetRootSuffixAsArgument(string rootSuffix)
            => string.IsNullOrWhiteSpace(rootSuffix) ? string.Empty : $"/rootSuffix {rootSuffix}";
    }
}
