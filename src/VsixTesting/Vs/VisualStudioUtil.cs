// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Vs
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Common;
    using Microsoft.VisualStudio.Setup.Configuration;
    using Microsoft.Win32;
    using VsixTesting.Utilities;

    internal static partial class VisualStudioUtil
    {
        public const string ProcessName = "devenv";

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

        public static async Task<(int InstallCount, string Output)> InstallExtensionsAsync(VsHive hive, IEnumerable<string> extensions)
        {
            var (result, output) = await RunExtensionInstallerAsync(hive, new[] { "/Install" }.Concat(extensions.Select(e => Quote(e))));
            return (result, output);
        }

        public static async Task<bool> IsProfileInitializedAsync(VsHive hive)
        {
            var (result, _) = await RunExtensionInstallerAsync(hive, new[] { "/IsProfileInitialized" });
            return result == 1 ? true : false;
        }

        public static async Task<(int Result, string Output)> RunExtensionInstallerAsync(VsHive hive, IEnumerable<string> args)
        {
            using (var visualStudioInstaller = new TempFile(EmbeddedResourceUtil.ExtractResource(Assembly.GetExecutingAssembly(), "VsixTesting.Installer.exe")))
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = visualStudioInstaller.Path,
                    Arguments = string.Join(" ", new string[]
                    {
                        "/ApplicationPath", Quote(hive.ApplicationPath),
                        "/RootSuffix", hive.RootSuffix,
                    }.Concat(args)),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                });

                await process.WaitForExitAsync();

                if (process.ExitCode < 0)
                    throw new Exception(process.StandardError.ReadToEnd());

                return (process.ExitCode, process.StandardOutput.ReadToEnd());
            }
        }

        internal static bool IsValidInstallationDirectory(string installationPath)
            => !string.IsNullOrWhiteSpace(installationPath) && File.Exists(GetApplicationPath(installationPath));

        internal static string GetApplicationPath(string installationPath)
            => Path.Combine(installationPath, "Common7", "IDE", ProcessName + ".exe");

        private static string GetRootSuffixAsArgument(string rootSuffix)
            => string.IsNullOrWhiteSpace(rootSuffix) ? string.Empty : $"/rootSuffix {rootSuffix}";

        private static string Quote(string str) => $"\"{str}\"";
    }
}
