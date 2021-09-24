// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.

namespace Vs
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Common;
    using VsixTesting.Utilities;

    internal static partial class VisualStudioUtil
    {
        public static async Task<(int InstallCount, string Output)> InstallExtensionsAsync(VsHive hive, IEnumerable<string> extensions)
        {
            var (result, output) = await RunInstallerAsync(hive, new[] { "/Install" }.Concat(extensions.Select(e => QuotePath(e))));
            return (result, output);
        }

        public static async Task<bool> IsProfileInitializedAsync(VsHive hive)
        {
            var (result, _) = await RunInstallerAsync(hive, new[] { "/IsProfileInitialized" });
            return result == 1 ? true : false;
        }

        public static async Task<(int Result, string Output)> RunInstallerAsync(VsHive hive, IEnumerable<string> args)
        {
            var suffix = hive.Version >= VersionUtil.FromVsVersion(VsVersion.VS2022) ? ".x64" : string.Empty;

            using (var visualStudioInstaller = new TempFile(EmbeddedResourceUtil.ExtractResource(Assembly.GetExecutingAssembly(), $"VsixTesting.Installer{suffix}.exe")))
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = visualStudioInstaller.Path,
                    Arguments = string.Join(" ", new string[]
                    {
                        "/ApplicationPath", QuotePath(hive.ApplicationPath),
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

        private static string QuotePath(string str) => $"\"{str}\"";
    }
}
