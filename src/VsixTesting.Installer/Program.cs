// Copyright (c) 2018 Jose Torres. All rights reserved. Licensed under the Apache License, Version 2.0. See LICENSE.md file in the project root for full license information.
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1201 // Elements should appear in the correct order

namespace VsixTesting.Installer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.CSharp.RuntimeBinder;
    using Microsoft.VisualStudio.ExtensionManager;
    using Vs;
    using VsixTesting.Utilities;
    using static VsixTesting.Installer.RestartManager;
    using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

    internal class Program : MarshalByRefObject
    {
        [STAThread]
        public static int Main(string[] args)
        {
            var applicationPath = CommandLineParser.One(args, "ApplicationPath");
            var rootSuffix = CommandLineParser.One(args, "RootSuffix", string.Empty);
            var appDomain = CreateAppDomain(applicationPath);
            var program = appDomain.CreateInstanceFromAndUnwrap<Program>();
            return program.Run(applicationPath, rootSuffix, args);
        }

        private int Run(string applicationPath, string rootSuffix, string[] args)
        {
            try
            {
                if (!ExtensionManagerUtil.IsValidProcessFileName(applicationPath, out var expectedFileName))
                    return StartProcess(expectedFileName);

                var commands = new Dictionary<string, Func<int>>
                {
                    {
                        "Install", () =>
                        {
                            var extensionPaths = CommandLineParser.Many(args, "Install");
                            return Installer.Install(applicationPath, rootSuffix, extensionPaths, allUsers: false);
                        }
                    },
                    {
                        "InstallAndStart", () =>
                        {
                            var extensionPaths = CommandLineParser.Many(args, "InstallAndStart");
                            var result = Installer.Install(applicationPath, rootSuffix, extensionPaths, allUsers: false);
                            using (var retryFilter = new RetryMessageFilter())
                            {
                                var dte = VisualStudioUtil.GetDteFromDebuggedProcess(Process.GetCurrentProcess());
                                var process = Process.Start(applicationPath, $"/RootSuffix {rootSuffix}");
                                if (dte != null)
                                   VisualStudioUtil.AttachDebugger(dte, process);
                                return result;
                            }
                        }
                    },
                    {
                        "Uninstall", () =>
                        {
                            var extensionIds = CommandLineParser.Many(args, "Uninstall");
                            return Installer.Uninstall(applicationPath, rootSuffix, extensionIds, skipGlobal: false);
                        }
                    },
                    {
                        "IsProfileInitialized", () =>
                        {
                            using (var externalSettingsManager = ExternalSettingsManager.CreateForApplication(applicationPath, rootSuffix))
                            {
                                var settings = externalSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                                var isProfileInitialized = settings.CollectionExists("Profile") && settings.GetPropertyNames("Profile").Contains("LastResetSettingsFile");
                                return Convert.ToInt32(isProfileInitialized);
                            }
                        }
                    },
                };

                foreach (var cmd in commands)
                {
                    if (CommandLineParser.Contains(args, cmd.Key))
                        return cmd.Value();
                }

                throw new Exception($@"Invalid command");
            }
            catch (Exception e)
            {
                Console.Error.Write(e.ToString());
                return -1;
            }
        }

        private static AppDomain CreateAppDomain(string applicationPath)
        {
            var version = Version.Parse(FileVersionInfo.GetVersionInfo(applicationPath).ProductVersion);
            var appDomainSetup = new AppDomainSetup { ApplicationBase = Path.GetDirectoryName(typeof(Program).Assembly.Location) };
            appDomainSetup.SetConfigurationBytes(Encoding.UTF8.GetBytes($@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
<runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
        <dependentAssembly>
            <assemblyIdentity name=""Microsoft.VisualStudio.ExtensionManager"" publicKeyToken=""b03f5f7f11d50a3a"" culture=""neutral"" />
            <bindingRedirect oldVersion=""10.0.0.0-{version.Major}.0.0.0"" newVersion=""{version.Major}.0.0.0"" />
        </dependentAssembly>
    </assemblyBinding>
    </runtime>
</configuration>"));
            var appDomain = AppDomain.CreateDomain($"{nameof(Installer)} {version}", null, appDomainSetup);
            var assemblyResolver = appDomain.CreateInstanceFromAndUnwrap<AssemblyResolver>();
            assemblyResolver.Install(Path.GetDirectoryName(applicationPath));
            return appDomain;
        }

        private static ResolveEventHandler CreateAssemblyResolver(string applicationDirectory)
        {
            var probingPaths = new[] { ".", "PrivateAssemblies", "PublicAssemblies" }
                .Select(relativeDir => Path.Combine(applicationDirectory, relativeDir));

            return (object sender, ResolveEventArgs eventArgs) =>
            {
                var assemblyName = new AssemblyName(eventArgs.Name);
                foreach (var probingPath in probingPaths)
                {
                    var assemblyFile = Path.Combine(probingPath, $"{assemblyName.Name}.dll");
                    if (File.Exists(assemblyFile))
                        return Assembly.LoadFrom(assemblyFile);
                }

                return null;
            };
        }

        private static int StartProcess(string filename)
        {
            var executablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
            File.Copy(Process.GetCurrentProcess().MainModule.FileName, executablePath, true);
            using (var retryFilter = new RetryMessageFilter())
            {
                var dte = VisualStudioUtil.GetDteFromDebuggedProcess(Process.GetCurrentProcess());
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = Environment.CommandLine,
                    UseShellExecute = false,
                });
                if (dte != null)
                    VisualStudioUtil.AttachDebugger(dte, process);
                process.WaitForExit();
                FileUtil.TryDelete(executablePath);
                return process.ExitCode;
            }
        }

        internal class AssemblyResolver : MarshalByRefObject
        {
            private ResolveEventHandler assemblyResolver;

            public void Install(string baseDir)
            {
                if (assemblyResolver == null)
                {
                    assemblyResolver = CreateAssemblyResolver(baseDir);
                    AppDomain.CurrentDomain.AssemblyResolve += assemblyResolver;
                }
            }
        }
    }

    internal class Installer : IDisposable
    {
        private readonly ExternalSettingsManager externalSettingsManager;
        private IVsExtensionManager extensionManager;

        public Installer(ExternalSettingsManager externalSettingsManager)
        {
            this.externalSettingsManager = externalSettingsManager;
            extensionManager = ExtensionManagerService.Create(externalSettingsManager);
        }

        internal ExtensionManagerService ExtensionManagerService => new ExtensionManagerService(extensionManager);

        public static int Install(string applicationPath, string rootSuffix, IEnumerable<string> extensionPaths, bool? allUsers = default)
        {
            using (var externalSettingsManager = ExternalSettingsManager.CreateForApplication(applicationPath, rootSuffix))
            {
                using (var installer = new Installer(externalSettingsManager))
                    return extensionPaths.Count(path => installer.Install(path, allUsers: allUsers));
            }
        }

        public bool Install(string extensionPath, bool? allUsers = default)
        {
            var installableExtension = extensionManager.CreateInstallableExtension(extensionPath);

            if (extensionManager.TryGetInstalledExtension(installableExtension.Header.Identifier, out var installedExtension))
            {
                if (IsUpToDate(installableExtension, installedExtension))
                {
                    Console.WriteLine($"Extension {NameVer(installedExtension)} is up to date.");
                    if (installedExtension.State != EnabledState.Enabled)
                        extensionManager.Enable(installedExtension);
                    return false;
                }

                Uninstall(installableExtension.Header.Identifier, skipGlobal: GetIsExperimentalProperty(installableExtension.Header) != null);
            }

            if (extensionManager.TryGetInstalledExtension(installableExtension.Header.Identifier, out var globalExtension) && globalExtension.InstalledPerMachine)
            {
                if (installableExtension.Header.Version <= globalExtension.Header.Version)
                    throw new Exception($"Extension '{NameVer(installableExtension)}' version must be higher than the globally installed extension '{NameVer(globalExtension)}'.");

                SetIsExperimental(installableExtension.Header, true);
            }

            if (allUsers.HasValue)
                SetAllUsers(installableExtension.Header, allUsers.Value);

            Console.WriteLine($"Installing {NameVer(installableExtension)}");
            extensionManager.Install(installableExtension, installableExtension.Header.AllUsers);
            ExtensionManagerUtil.EnableLoadingExtensionsFromLocalAppData(externalSettingsManager);
            ExtensionManagerUtil.RemovePendingExtensionDeletion(externalSettingsManager, installableExtension.Header);
            ExtensionManagerService.UpdateLastExtensionsChange();
            var newlyInstalledExtension = extensionManager.GetInstalledExtension(installableExtension.Header.Identifier);
            extensionManager.Enable(newlyInstalledExtension);
            File.SetLastWriteTime(GetManifestPath(newlyInstalledExtension), DateTime.Now);
            if (!IsUpToDate(installableExtension, newlyInstalledExtension))
                throw new Exception($"Failed installing extension '{NameVer(installableExtension)}'.");
            return true;
        }

        public static int Uninstall(string applicationPath, string rootSuffix, IEnumerable<string> extensionIds, bool skipGlobal)
        {
            using (var externalSettingsManager = ExternalSettingsManager.CreateForApplication(applicationPath, rootSuffix))
            {
                using (var installer = new Installer(externalSettingsManager))
                    return extensionIds.Count(id => installer.Uninstall(id, skipGlobal));
            }
        }

        public bool Uninstall(string id, bool skipGlobal)
        {
            if (!extensionManager.TryGetInstalledExtension(id, out var _))
                return false;

            while (extensionManager.TryGetInstalledExtension(id, out var installedExtension))
            {
                if (skipGlobal && installedExtension.InstalledPerMachine)
                    return false;

                Console.WriteLine($"Uninstalling {NameVer(installedExtension)}");
                extensionManager.Uninstall(installedExtension);

                DirectoryUtil.DeleteHard(installedExtension.InstallPath, true);
                ExtensionManagerUtil.RemovePendingExtensionDeletion(externalSettingsManager, installedExtension.Header);

                // Reset extension manager cache
                ExtensionManagerService.Dispose();
                extensionManager = ExtensionManagerService.Create(externalSettingsManager);
            }

            return true;
        }

        public void Dispose() => ExtensionManagerService.Dispose();

        private static bool IsUpToDate(IInstallableExtension installableExtension, IInstalledExtension installedExtension)
        {
            return
                installedExtension.Header.Version == installableExtension.Header.Version &&
                File.GetLastWriteTime(GetManifestPath(installedExtension)) >= File.GetLastWriteTime(installableExtension.PackagePath) &&
                installedExtension.InstalledPerMachine == false;
        }

        private static string NameVer(IExtension extension) => extension.Header.Name + " " + extension.Header.Version;

        private static string GetManifestPath(IInstalledExtension installedExtension)
            => Path.Combine(installedExtension.InstallPath, "extension.vsixmanifest");

        private static void SetIsExperimental(IExtensionHeader header, bool value)
        {
            var prop = GetIsExperimentalProperty(header);
            if (prop != null && prop.CanWrite)
                prop.SetValue(header, value);
        }

        private static void SetAllUsers(IExtensionHeader header, bool value)
        {
            var prop = header.GetType().GetProperty(nameof(header.AllUsers));
            if (prop != null && prop.CanWrite)
                prop.SetValue(header, value);
        }

        private static PropertyInfo GetIsExperimentalProperty(IExtensionHeader header)
            => header.GetType().GetProperty("IsExperimental");
    }

    internal class ExtensionManagerService
    {
        public ExtensionManagerService(IVsExtensionManager obj)
        {
            if (obj.GetType() != GetRealType())
                throw new InvalidCastException();

            Obj = obj;
        }

        public dynamic Obj { get; }

        public static string VsProductVersion
        {
            get => (string)GetRealType().GetProperty(nameof(VsProductVersion)).GetValue(null);
            set => GetRealType().GetProperty(nameof(VsProductVersion)).SetValue(null, value);
        }

        public static IVsExtensionManager Create(ExternalSettingsManager externalSettingsManager)
        {
            return (IVsExtensionManager)GetRealType()
                .GetConstructor(new Type[] { externalSettingsManager.Obj.GetType() })
                .Invoke(new[] { externalSettingsManager.Obj });
        }

        public void UpdateLastExtensionsChange()
        {
            try
            {
                Obj.UpdateLastExtensionsChange();
            }
            catch (RuntimeBinderException)
            {
                Obj.UpdateLastExtensionsChange(true);
            }
        }

        public void Close()
            => Obj.Close();

        public void Dispose()
        {
            Close();
            (Obj as IDisposable)?.Dispose();
        }

        private static Type GetRealType()
        {
            var majorVersion = typeof(IVsExtensionManager).Assembly.GetName().Version.Major;

            var assembly = Assembly.Load($"Microsoft.VisualStudio.ExtensionManager.Implementation, " +
                $"Version={majorVersion}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            return assembly.GetType("Microsoft.VisualStudio.ExtensionManager.ExtensionManagerService");
        }
    }

    internal class ExternalSettingsManager : IDisposable
    {
        public ExternalSettingsManager(object obj)
        {
            Obj = obj;
        }

        public object Obj { get; }

        public static ExternalSettingsManager CreateForApplication(string applicationPath, string rootSuffix)
        {
            return new ExternalSettingsManager(GetRealType()
                .GetMethod(nameof(CreateForApplication), new[] { typeof(string), typeof(string) })
                .Invoke(null, new object[] { applicationPath, rootSuffix }));
        }

        public WritableSettingsStore GetWritableSettingsStore(SettingsScope scope)
        {
            var method = Obj.GetType().GetMethods().First(
                m => m.Name == nameof(GetWritableSettingsStore) &&
                m.GetParameters().Length == 1 &&
                m.GetParameters().Single().ParameterType.Name == nameof(SettingsScope));

            return new WritableSettingsStore(method.Invoke(Obj, new object[] { scope }));
        }

        public void Dispose()
            => ((IDisposable)Obj).Dispose();

        private static Type GetRealType()
        {
            var majorVersion = typeof(IVsExtensionManager).Assembly.GetName().Version.Major;

            string suffix;
            int assemblyVersion;
            switch (majorVersion)
            {
                case 10:
                    suffix = string.Empty;
                    assemblyVersion = 10;
                    break;

                case 16:
                    suffix = ".15.0";
                    assemblyVersion = 15;
                    break;

                default:
                    suffix = $".{majorVersion}.0";
                    assemblyVersion = majorVersion;
                    break;
            }

            var assembly = Assembly.Load($"Microsoft.VisualStudio.Settings{suffix}, " +
                $"Version={assemblyVersion}.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            return assembly.GetType("Microsoft.VisualStudio.Settings.ExternalSettingsManager");
        }
    }

    internal class ExtensionManagerUtil
    {
        private const string ExtensionManagerCollectionPath = "ExtensionManager";

        public static void EnableLoadingExtensionsFromLocalAppData(ExternalSettingsManager externalSettingsManager)
        {
            var settingsStore = externalSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(ExtensionManagerCollectionPath))
                settingsStore.CreateCollection(ExtensionManagerCollectionPath);

            settingsStore.SetBoolean(ExtensionManagerCollectionPath, "EnableAdminExtensions", true);
        }

        public static void RemovePendingExtensionDeletion(ExternalSettingsManager externalSettingsManager, IExtensionHeader extensionHeader)
        {
            var settingsStore = externalSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            var collectionPath = $"{ExtensionManagerCollectionPath}\\PendingDeletions";

            if (settingsStore.CollectionExists(collectionPath))
            {
                var idver = $"{extensionHeader.Identifier},{extensionHeader.Version}";

                if (settingsStore.PropertyExists(collectionPath, idver))
                    settingsStore.DeleteProperty(collectionPath, idver);
            }
        }

        internal static bool IsValidProcessFileName(string applicationPath, out string expectedFileName)
        {
            var productVersion = Version.Parse(FileVersionInfo.GetVersionInfo(applicationPath).ProductVersion);

            if (productVersion.Major == 15 && productVersion.Minor == 8)
            {
                try
                {
                    ExtensionManagerService.VsProductVersion = productVersion.ToString();
                    var value = ExtensionManagerService.VsProductVersion;
                }
                catch
                {
                    expectedFileName = "ServiceHub.VSDetouredHost.exe";
                    return false; // [15.8.3-15.8.5]
                }
            }

            expectedFileName = null;
            return true;
        }
    }

    internal class WritableSettingsStore
    {
        private dynamic obj;

        public WritableSettingsStore(dynamic obj)
        {
            var type = (Type)obj.GetType();

            while (type.BaseType != null)
            {
                if (type.BaseType.FullName == "Microsoft.VisualStudio.Settings.WritableSettingsStore")
                {
                    this.obj = obj;
                    return;
                }
            }

            throw new InvalidCastException();
        }

        public bool CollectionExists(string collectionPath)
            => obj.CollectionExists(collectionPath);

        public IEnumerable<string> GetPropertyNames(string collectionPath)
            => obj.GetPropertyNames(collectionPath);

        public bool PropertyExists(string collectionPath, string propertyName)
            => obj.PropertyExists(collectionPath, propertyName);

        public bool DeleteProperty(string collectionPath, string propertyName)
            => obj.DeleteProperty(collectionPath, propertyName);

        public void CreateCollection(string collectionPath)
            => obj.CreateCollection(collectionPath);

        public void SetBoolean(string collectionPath, string propertyName, bool value)
            => obj.SetBoolean(collectionPath, propertyName, value);
    }

    internal enum SettingsScope
    {
        Configuration = 1,
        UserSettings = 2,
        Remote = 4,
    }

    internal class CommandLineParser
    {
        internal static IEnumerable<string> Many(string[] args, string name)
        {
            for (var i = 0; i < args.Length; ++i)
            {
                if (!args[i].Equals($"/{name}", StringComparison.OrdinalIgnoreCase))
                    continue;

                while (++i < args.Length)
                {
                    if (args[i].StartsWith("/"))
                        yield break;
                    yield return args[i];
                }
            }
        }

        internal static string One(string[] args, string name, string @default = null)
            => Many(args, name).FirstOrDefault() ?? @default ?? throw new ArgumentException($"/{name} is required.");

        internal static bool Contains(string[] args, string name)
            => args.Contains($"/{name}", StringComparer.OrdinalIgnoreCase) ? true : false;
    }

    internal static class DirectoryUtil
    {
        public static void DeleteHard(string path, bool recursive, int timeoutMs = 10_000)
        {
            var processes = ResourceManagerUtil.GetProcessesLockingResources(Directory.GetFiles(path));

            foreach (var process in processes)
            {
                process.CloseMainWindow();
                process.WaitForExit(timeoutMs);
            }

            Directory.Delete(path, recursive);
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

    internal static class AppDomainExtensions
    {
        public static T CreateInstanceFromAndUnwrap<T>(this AppDomain domain)
            => (T)domain.CreateInstanceFromAndUnwrap(typeof(T).Assembly.Location, typeof(T).FullName);
    }
}
