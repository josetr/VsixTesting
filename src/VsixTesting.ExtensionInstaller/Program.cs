// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1402 // File may only contain a single type

namespace VsixTesting.ExtensionInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.ExtensionManager;
    using Microsoft.Win32;
    using static RestartManager;
    using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

    internal class Program
    {
        internal static Version VsVersion { get; private set; } = new Version(11, 0);

        public static int Main(string[] args)
        {
            try
            {
                var path = CommandLineParser.One(args, "ApplicationPath");
                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                VsVersion = Version.Parse(versionInfo.ProductVersion);
                var rootSuffix = CommandLineParser.One(args, "RootSuffix");
                var extensionPaths = CommandLineParser.Many(args, "ExtensionPaths");

                if (TryStartRealProcess(out var ec))
                    return ec;

                AppDomain.CurrentDomain.AssemblyResolve += CreateAssemblyResolver(Path.GetDirectoryName(path));

                var externalSettingsManager = ExternalSettingsManager.CreateForApplication(path, rootSuffix);
                var extensionManagerService = ExtensionManagerService.Create(externalSettingsManager);
                ExtensionManagerService.VsProductVersion = versionInfo.ProductVersion;
                var installer = new Installer(extensionManagerService);
                var result = installer.InstallExtensions(extensionPaths);
                var hiveId = Path.GetFileName(externalSettingsManager.GetApplicationDataFolder(0));

                return HiveHasSettingsFile(hiveId)
                    ? result
                    : result + 9999;
            }
            catch (Exception e)
            {
                Console.Error.Write(e.ToString());
                return -1;
            }
        }

        private static bool TryStartRealProcess(out int ec)
        {
            var appFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServiceHub.VSDetouredHost.exe");
            var appConfigFilePath = appFilePath + ".config";
            ec = 0;

            if (string.Equals(Process.GetCurrentProcess().MainModule.ModuleName, "ServiceHub.VSDetouredHost.exe", StringComparison.OrdinalIgnoreCase))
                return false;

            var appConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
<runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
        <dependentAssembly>
            <assemblyIdentity name=""Microsoft.VisualStudio.ExtensionManager"" publicKeyToken=""b03f5f7f11d50a3a"" culture=""neutral"" />
            <bindingRedirect oldVersion=""10.0.0.0-{VsVersion.Major}.0.0.0"" newVersion=""{VsVersion.Major}.0.0.0"" />
        </dependentAssembly>
    </assemblyBinding>
    </runtime>
</configuration>";

            File.Copy(Process.GetCurrentProcess().MainModule.FileName, appFilePath, true);
            File.WriteAllText(appConfigFilePath, appConfig);

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = appFilePath,
                Arguments = Environment.CommandLine,
                UseShellExecute = false,
            });

            process.WaitForExit();
            FileUtil.TryDelete(appConfigFilePath);
            FileUtil.TryDelete(appConfig);
            ec = process.ExitCode;
            return true;
        }

        private static ResolveEventHandler CreateAssemblyResolver(string applicationDirectory)
        {
            return (object sender, ResolveEventArgs eventArgs) =>
            {
                var assemblyName = new AssemblyName(eventArgs.Name);
                foreach (var sdir in new string[] { ".", "PrivateAssemblies", "PublicAssemblies" })
                {
                    var assemblyFile = Path.Combine(applicationDirectory, sdir, $"{assemblyName.Name}.dll");
                    if (File.Exists(assemblyFile))
                        return Assembly.LoadFrom(assemblyFile);
                }

                return null;
            };
        }

        private static bool HiveHasSettingsFile(string id)
        {
            using (var profile = Registry.CurrentUser.OpenSubKey($@"SOFTWARE\Microsoft\VisualStudio\{id}\Profile"))
            {
                if (profile != null && !string.IsNullOrEmpty(profile.GetValue("LastResetSettingsFile") as string))
                    return true;
            }

            return false;
        }
    }

    internal class Installer
    {
        private readonly IVsExtensionManager extensionManager;

        public Installer(IVsExtensionManager extensionManager)
        {
            this.extensionManager = extensionManager;
        }

        public int InstallExtensions(IEnumerable<string> extensionPaths)
            => extensionPaths.Where(extensionPath => InstallExtension(extensionPath)).Count();

        public bool InstallExtension(string extensionPath)
        {
            var installableExtension = extensionManager.CreateInstallableExtension(extensionPath);
            var packageWriteDateTime = File.GetLastWriteTime(installableExtension.PackagePath);
            var installedExtension = default(IInstalledExtension);

            if (extensionManager.IsInstalled(installableExtension))
            {
                installedExtension = extensionManager.GetInstalledExtension(installableExtension.Header.Identifier);
                var installedOn = File.GetLastWriteTime(GetManifestPath(installedExtension)); // installedExtension.InstalledOn throws

                if (installableExtension.Header.Version == installedExtension.Header.Version && packageWriteDateTime < installedOn)
                {
                    Console.WriteLine($"Extension {NameVer(installedExtension)} is up to date.");
                    extensionManager.Enable(installedExtension);
                    return false;
                }

                Console.WriteLine($"Uninstalling {NameVer(installedExtension)}");
                extensionManager.Uninstall(installedExtension);
            }

            if (installableExtension.Header.AllUsers)
            {
                var header = installableExtension.Header;
                header.GetType().GetProperty(nameof(header.AllUsers)).SetValue(header, false);
            }

            Console.WriteLine($"Installing {NameVer(installableExtension)}");
            extensionManager.Install(installableExtension, false);

            var newlyInstalledExtension = extensionManager.GetInstalledExtension(installableExtension.Header.Identifier);
            extensionManager.Enable(newlyInstalledExtension);
            File.SetLastWriteTime(GetManifestPath(newlyInstalledExtension), DateTime.Now);

            if (installedExtension != null && !PathUtil.ArePathEqual(installedExtension.InstallPath, newlyInstalledExtension.InstallPath))
            {
                Console.WriteLine($"Removing {installedExtension.InstallPath}");

                if (!DirectoryUtil.TryDeleteHard(installedExtension.InstallPath, recursive: true))
                    Console.WriteLine($"The directory {installedExtension.InstallPath} could not be deleted completely. If you do not delete it manually, it will waste disk space.");
            }

            return true;
        }

        private static string NameVer(IExtension ext) => ext.Header.Name + " " + ext.Header.Version;

        private static string GetManifestPath(IInstalledExtension installedExtension)
            => Path.Combine(installedExtension.InstallPath, "extension.vsixmanifest");
    }

    internal class ExtensionManagerService
    {
        public static string VsProductVersion
        {
            set => GetRealType().GetProperty("VsProductVersion")?.SetValue(null, value);
        }

        public static Type GetRealType()
        {
            var assembly = Assembly.Load($"Microsoft.VisualStudio.ExtensionManager.Implementation, " +
                $"Version={Program.VsVersion.Major}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            return assembly.GetType("Microsoft.VisualStudio.ExtensionManager.ExtensionManagerService");
        }

        public static IVsExtensionManager Create(object externalSettingsManager)
        {
            return (IVsExtensionManager)GetRealType()
                .GetConstructor(new[] { externalSettingsManager.GetType() })
                .Invoke(new[] { externalSettingsManager });
        }
    }

    internal class ExternalSettingsManager
    {
        public static Type GetRealType()
        {
            var assembly = Assembly.Load($"Microsoft.VisualStudio.Settings{(Program.VsVersion.Major > 10 ? $".{Program.VsVersion.Major}.0" : string.Empty)}, " +
                $"Version={Program.VsVersion.Major}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            return assembly.GetType("Microsoft.VisualStudio.Settings.ExternalSettingsManager");
        }

        public static dynamic CreateForApplication(string applicationPath, string rootSuffix)
        {
            return GetRealType()
                .GetMethod("CreateForApplication", new[] { typeof(string), typeof(string) })
                .Invoke(null, new object[] { applicationPath, rootSuffix });
        }
    }

    internal class CommandLineParser
    {
        internal static IEnumerable<string> Many(string[] args, string name)
        {
            for (var i = 0; i < args.Length; ++i)
            {
                if (!args[i].Equals($"--{name}", StringComparison.OrdinalIgnoreCase))
                    continue;

                while (++i < args.Length)
                {
                    if (args[i].StartsWith("--"))
                        yield break;
                    yield return args[i];
                }
            }
        }

        internal static string One(string[] args, string name)
            => Many(args, name).FirstOrDefault() ?? throw new ArgumentException($"Argument --{name} is required.");
    }

    internal static class DirectoryUtil
    {
        public static bool TryDeleteHard(string path, bool recursive, int timeoutMs = 10_000)
        {
            try
            {
                try
                {
                    Directory.Delete(path, recursive);
                }
                catch
                {
                    foreach (var file in Directory.GetFiles(path))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            try
                            {
                                foreach (var process in ResourceManagerUtil.GetProcessesLockingResources(file))
                                {
                                    process.CloseMainWindow();
                                    process.WaitForExit(timeoutMs);
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                    Directory.Delete(path, recursive);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    internal static class FileUtil
    {
        public static bool TryDelete(string path)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch
            {
            }

            return false;
        }
    }

    internal static class PathUtil
    {
        public static bool ArePathEqual(string path1, string path2, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
            => string.Equals(NormalizePath(path1), NormalizePath(path2), comparisonType);

        private static string NormalizePath(string path)
        {
            path = Path.GetFullPath(path);
            for (var i = path.Length - 1; i >= 0; i--)
            {
                if (path[i] != Path.DirectorySeparatorChar && path[i] != Path.AltDirectorySeparatorChar)
                    return path.Substring(0, i + 1);
            }

            return string.Empty;
        }
    }

    internal static class SystemErrorCodes
    {
        public const int ERROR_SUCCESS = 0;
        public const int ERROR_MORE_DATA = 234;
    }

    internal static class ResourceManagerUtil
    {
        public static IEnumerable<Process> GetProcessesLockingResources(params string[] resources)
        {
            Succeeded("RmStartSession", RmStartSession(
                pSessionHandle: out var dwSessionHandle,
                dwSessionFlags: 0,
                strSessionKey: Guid.NewGuid().ToString()));

            try
            {
                Succeeded("RmRegisterResources", RmRegisterResources(dwSessionHandle, (uint)resources.Length, resources, 0, null, 0, null));

                var nProcInfoNeeded = 0U;
                var dwRebootReasons = (uint)RM_REBOOT_REASON.RmRebootReasonNone;
                var procInfos = default(RM_PROCESS_INFO[]);
                var result = 0;

                do
                {
                    procInfos = new RM_PROCESS_INFO[nProcInfoNeeded];
                    result = RmGetList(dwSessionHandle, out nProcInfoNeeded, ref nProcInfoNeeded, procInfos, ref dwRebootReasons);
                }
                while (result == SystemErrorCodes.ERROR_MORE_DATA);

                Succeeded("RmGetList", result);

                foreach (var procInfo in procInfos)
                {
                    var process = default(Process);
                    try
                    {
                        process = Process.GetProcessById(procInfo.Process.dwProcessId);
                    }
                    catch
                    {
                    }

                    if (process != default(Process))
                        yield return process;
                }
            }
            finally
            {
                Succeeded("RmEndSession", RmEndSession(dwSessionHandle));
            }

            void Succeeded(string name, int err)
            {
                if (err != SystemErrorCodes.ERROR_SUCCESS)
                    throw new Exception($"Cannot find who's locking the resources {string.Join("/", resources)}. {name} failed with error {err}.");
            }
        }
    }

    internal static class RestartManager
    {
        public const int CCH_RM_MAX_APP_NAME = 255;
        public const int CCH_RM_MAX_SVC_NAME = 63;

        public enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000,
        }

        public enum RM_REBOOT_REASON
        {
            RmRebootReasonNone,
            RmRebootReasonPermissionDenied,
            RmRebootReasonSessionMismatch,
            RmRebootReasonCriticalProcess,
            RmRebootReasonCriticalService,
            RmRebootReasonDetectedSelf,
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        public static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        public static extern int RmRegisterResources(uint dwSessionHandle, uint nFiles, string[] rgsFilenames, uint nApplications, [In] RM_UNIQUE_PROCESS[] rgApplications, uint nServices, string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll")]
        public static extern int RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded, ref uint pnProcInfo, [In, Out] RM_PROCESS_INFO[] rgAffectedApps, ref uint lpdwRebootReasons);

        [DllImport("rstrtmgr.dll")]
        public static extern int RmEndSession(uint dwSessionHandle);

        [StructLayout(LayoutKind.Sequential)]
        public struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public FILETIME ProcessStartTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public string strAppName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public string strServiceShortName;
            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }
    }
}
