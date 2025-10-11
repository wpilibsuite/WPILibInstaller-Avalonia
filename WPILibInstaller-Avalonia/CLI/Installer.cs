using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Models.CLI;
using WPILibInstaller.Utils;
using static WPILibInstaller.Utils.ArchiveUtils;

namespace WPILibInstaller.CLI
{
    public class Installer
    {
        private readonly IConfigurationProvider configurationProvider;

        private readonly CLIInstallSelectionModel installSelectionModel;

        public Installer(string[] args)
        {
            var parser = new Parser(args);
            configurationProvider = parser.configurationProvider;
            installSelectionModel = parser.installSelectionModel;
        }

        public async Task Install()
        {
            Console.WriteLine("Extracting");
            await ExtractArchive(null);
            Console.WriteLine("Installing Gradle");
            await RunGradleSetup();
            if (installSelectionModel.InstallTools && installSelectionModel.InstallWPILibDeps) {
                Console.WriteLine("Installing Tools");
                await RunToolSetup();
            }
            Console.WriteLine("Installing CPP");
            await RunCppSetup();
            Console.WriteLine("Fixing Maven");
            await RunMavenMetaDataFixer();
            Console.WriteLine("Installing VS Code");
            await RunVsCodeSetup();
            Console.WriteLine("Configuring VS Code");
            await ConfigureVsCodeSettings();
            Console.WriteLine("Installing VS Code Extensions");
            await RunVsCodeExtensionsSetup();
            Console.WriteLine("Creating Shortcuts");
            await RunShortcutCreator();
        }

        private void SetExecutableIfNeeded(string fullZipToPath, bool entryIsExecutable)
        {
            if (!entryIsExecutable)
            {
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows doesn't use executable bits, nothing to do
                return;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/chmod",
                    Arguments = $"+x \"{fullZipToPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(startInfo);
                proc!.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    Console.WriteLine($"Warning: chmod failed for {fullZipToPath} (exit code {proc.ExitCode})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: unable to set executable bit for {fullZipToPath}: {ex.Message}");
                throw;
            }
        }


        private async Task ExtractArchive(string[]? filter)
        {
            if (OperatingSystem.IsWindows())
            {
                bool foundRunningExe = await Task.Run(() =>
                {
                    try
                    {
                        var jdkBinFolder = Path.Join(configurationProvider.InstallDirectory, configurationProvider.JdkConfig.Folder, "bin");
                        var jdkExes = Directory.EnumerateFiles(jdkBinFolder, "*.exe", SearchOption.AllDirectories);
                        bool found = false;
                        foreach (var exe in jdkExes)
                        {
                            try
                            {
                                var name = Path.GetFileNameWithoutExtension(exe)!;
                                var pNames = Process.GetProcessesByName(name);
                                foreach (var p in pNames)
                                {
                                    if (p.MainModule?.FileName == exe)
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (found)
                                {
                                    break;
                                }
                            }
                            catch
                            {
                                // Do nothing. We don't want this code to break.
                            }
                        }
                        return found;
                    }
                    catch
                    {
                        // Do nothing. We don't want this code to break.
                        return false;
                    }
                });
                if (foundRunningExe)
                {
                    string msg = "Running JDK processes have been found. Installation cannot continue. Please restart your computer, and rerun this installer without running anything else (including VS Code)";
                    throw new InvalidOperationException(msg);
                }
            }

            var archive = configurationProvider.ZipArchive;

            var extractor = archive;

            double totalSize = extractor.TotalUncompressSize;
            long currentSize = 0;

            string intoPath = configurationProvider.InstallDirectory;

            while (extractor.MoveToNextEntry())
            {
                currentSize += extractor.EntrySize;
                if (extractor.EntryIsDirectory) continue;

                var entryName = extractor.EntryKey;
                if (filter != null)
                {
                    bool skip = true;
                    foreach (var keep in filter)
                    {
                        if (entryName.StartsWith(keep))
                        {
                            skip = false;
                            break;
                        }
                    }

                    if (skip)
                    {
                        continue;
                    }
                }

                double currentPercentage = (currentSize / totalSize) * 100;
                if (currentPercentage > 100) currentPercentage = 100;
                if (currentPercentage < 0) currentPercentage = 0;

                string fullZipToPath = Path.Combine(intoPath, entryName);
                string? directoryName = Path.GetDirectoryName(fullZipToPath);
                if (directoryName?.Length > 0)
                {
                    try
                    {
                        Directory.CreateDirectory(directoryName);
                    }
                    catch (IOException)
                    {

                    }
                }

                {
                    using FileStream writer = File.Create(fullZipToPath);
                    await extractor.CopyToStreamAsync(writer);
                }

                if (extractor.EntryIsExecutable && !OperatingSystem.IsWindows())
                {
                    var currentMode = File.GetUnixFileMode(fullZipToPath);
                    File.SetUnixFileMode(fullZipToPath, currentMode | UnixFileMode.GroupExecute | UnixFileMode.UserExecute | UnixFileMode.OtherExecute);
                }
            }

        }

        private List<string> GetExtractionIgnoreDirectories()
        {
            List<string> ignoreDirs = new List<string>();
            if (!installSelectionModel.InstallCpp) ignoreDirs.Add(configurationProvider.FullConfig.CppToolchain.Directory + "/");
            if (!installSelectionModel.InstallGradle) ignoreDirs.Add(configurationProvider.FullConfig.Gradle.ZipName);
            if (!installSelectionModel.InstallJDK) ignoreDirs.Add(configurationProvider.JdkConfig.Folder + "/");
            if (!installSelectionModel.InstallTools) ignoreDirs.Add(configurationProvider.UpgradeConfig.Tools.Folder + "/");
            if (!installSelectionModel.InstallWPILibDeps) ignoreDirs.Add(configurationProvider.UpgradeConfig.Maven.Folder + "/");

            return ignoreDirs;
        }

        private Task RunGradleSetup()
        {
            if (!installSelectionModel.InstallGradle || !installSelectionModel.InstallWPILibDeps) return Task.CompletedTask;

            string extractFolder = configurationProvider.InstallDirectory;

            var config = configurationProvider.FullConfig;

            string gradleZipLoc = Path.Combine(extractFolder, "installUtils", config.Gradle.ZipName);

            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            List<Task> tasks = new List<Task>();
            foreach (var extractLocation in config.Gradle.ExtractLocations)
            {
                string toFolder = Path.Combine(userFolder, ".gradle", extractLocation, Path.GetFileNameWithoutExtension(config.Gradle.ZipName), config.Gradle.Hash);
                string toFile = Path.Combine(toFolder, config.Gradle.ZipName);
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Directory.CreateDirectory(toFolder);
                    }
                    catch (IOException)
                    {

                    }
                    File.Copy(gradleZipLoc, toFile, true);
                }));
            }
            return Task.WhenAll(tasks);
        }

        private async Task RunToolSetup()
        {
            if (!installSelectionModel.InstallTools || !installSelectionModel.InstallWPILibDeps)
                return;

            await RunJavaJar(configurationProvider.InstallDirectory,
                Path.Combine(configurationProvider.InstallDirectory,
                configurationProvider.UpgradeConfig.Tools.Folder,
                configurationProvider.UpgradeConfig.Tools.UpdaterJar), 20000);
        }

        private static Task<bool> RunJavaJar(string installDir, string jar, int timeoutMs)
        {
            string java = Path.Join(installDir, "jdk", "bin", "java");
            if (OperatingSystem.IsWindows())
            {
                java += ".exe";
            }
            ProcessStartInfo pstart = new ProcessStartInfo(java, $"-jar \"{jar}\"");
            pstart.RedirectStandardOutput = true;
            var p = Process.Start(pstart);
            return Task.Run(() =>
            {
                return p!.WaitForExit(timeoutMs);
            });
        }

        private Task<bool> RunScriptExecutable(string script, int timeoutMs, params string[] args)
        {
            ProcessStartInfo pstart = new ProcessStartInfo(script, string.Join(" ", args));
            var p = Process.Start(pstart);
            return Task.Run(() =>
            {
                return p!.WaitForExit(timeoutMs);
            });
        }

        private async Task RunCppSetup()
        {
            if (!installSelectionModel.InstallCpp) return;

            await Task.Yield();
        }

        private async Task RunMavenMetaDataFixer()
        {
            if (!installSelectionModel.InstallWPILibDeps)
                return;

            await RunJavaJar(configurationProvider.InstallDirectory,
                Path.Combine(configurationProvider.InstallDirectory,
                configurationProvider.UpgradeConfig.Maven.Folder,
                configurationProvider.UpgradeConfig.Maven.MetaDataFixerJar), 20000);
        }

        private async Task<(MemoryStream stream, Platform platform, byte[] hash)> DownloadToMemoryStream(Platform platform, string downloadUrl)
        {
            MemoryStream ms = new MemoryStream(100000000);
            // Download VS Code for current platform
            {
                using var client = new HttpClientDownloadWithProgress(downloadUrl, ms);
                await client.StartDownload();
            }

            // Compute hash of download
            ms.Seek(0, SeekOrigin.Begin);
            using var sha = SHA256.Create();
            var hash = await sha.ComputeHashAsync(ms);
            return (ms, platform, hash);
        }

        private async Task DownloadVsCode()
        {
            var currentPlatform = PlatformUtils.CurrentPlatform;
            var url = configurationProvider.VsCodeModel.Platforms[currentPlatform].DownloadUrl;

            var (stream, platform, hash) = await DownloadToMemoryStream(currentPlatform, url);

            if (!hash.AsSpan().SequenceEqual(configurationProvider.VsCodeModel.Platforms[platform].Sha256Hash))
            {
                throw new InvalidDataException("Invalid hash for VSCode download");
            }

            if (OperatingSystem.IsMacOS())
            {
                configurationProvider.VsCodeModel.ToExtractArchiveMacOs = stream;
            }
            else
            {
                configurationProvider.VsCodeModel.ToExtractArchive = OpenArchive(stream);
            }
        }

        private async Task RunVsCodeSetup()
        {
            if (!installSelectionModel.InstallVsCode) return;

            await DownloadVsCode();

            string intoPath = Path.Join(configurationProvider.InstallDirectory, "vscode");

            if (configurationProvider.VsCodeModel.ToExtractArchiveMacOs != null)
            {
                // MacOS Behavior
                configurationProvider.VsCodeModel.ToExtractArchiveMacOs.Seek(0, SeekOrigin.Begin);
                var zipPath = Path.Join(intoPath, "MacVsCode.zip");
                Directory.CreateDirectory(intoPath);
                {
                    using var fileToWrite = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await configurationProvider.VsCodeModel.ToExtractArchiveMacOs.CopyToAsync(fileToWrite);
                }
                await RunScriptExecutable("unzip", Timeout.Infinite, "-qq", zipPath, "-d", intoPath);
                File.Delete(zipPath);
                return;
            }
            else if (configurationProvider.VsCodeModel.ToExtractArchive != null)
            {
                var archive = configurationProvider.VsCodeModel.ToExtractArchive!;

                var extractor = archive;

                // FIXME: archive is, in fact, null on MacOS
                if (archive == null)
                {
                    throw new Exception("archive is null.");
                }

                while (extractor.MoveToNextEntry())
                {
                    if (extractor == null)
                    {
                        Console.WriteLine("extractor is null");
                        break;
                    }

                    if (extractor.EntryIsDirectory)
                        continue;

                    var entryName = extractor.EntryKey;
                    if (entryName == null)
                    {
                        Console.WriteLine("entryName is null");
                        break;
                    }

                    string fullZipToPath = Path.Combine(intoPath, entryName);
                    string? directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName?.Length > 0)
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    {
                        using FileStream writer = File.Create(fullZipToPath);
                        await extractor.CopyToStreamAsync(writer);
                    }
                    SetExecutableIfNeeded(fullZipToPath, extractor.EntryIsExecutable);
                }
            }
            else
            {
                throw new Exception("ToExtractArchive and ToExtractArchiveMacOs are both null.");
            }
        }

        private async Task ConfigureVsCodeSettings()
        {
            if (!installSelectionModel.InstallVsCode && !configurationProvider.VsCodeModel.AlreadyInstalled) return;

            var dataPath = await SetVsCodePortableMode();

            var settingsDir = Path.Combine(dataPath, "user-data", "User");
            var settingsFile = Path.Combine(settingsDir, "settings.json");

            var homePath = configurationProvider.InstallDirectory;

            var codeFolder = Path.Combine(homePath, configurationProvider.UpgradeConfig.PathFolder);

            try
            {
                Directory.CreateDirectory(codeFolder);
            }
            catch (IOException)
            {

            }

            try
            {
                Directory.CreateDirectory(settingsDir);
            }
            catch (IOException)
            {

            }

            dynamic settingsJson = new JObject();
            if (File.Exists(settingsFile))
            {
                settingsJson = (JObject)JsonConvert.DeserializeObject(await File.ReadAllTextAsync(settingsFile))!;
            }

            SetIfNotSet("java.home", Path.Combine(homePath, "jdk"), settingsJson);
            SetIfNotSet("extensions.autoUpdate", false, settingsJson);
            SetIfNotSet("extensions.autoCheckUpdates", false, settingsJson);
            SetIfNotSet("extensions.ignoreRecommendations", true, settingsJson);
            SetIfNotSet("extensions.showRecommendationsOnlyOnDemand", false, settingsJson);
            SetIfNotSet("update.channel", "none", settingsJson);
            SetIfNotSet("update.showReleaseNotes", false, settingsJson);

            string os;
            string path_seperator;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                os = "windows";
                path_seperator = ";";
            }
            else if (OperatingSystem.IsMacOS())
            {
                os = "osx";
                path_seperator = ":";
            }
            else
            {
                os = "linux";
                path_seperator = ":";
            }

            if (!settingsJson.ContainsKey("terminal.integrated.env." + os))
            {
                dynamic terminalProps = new JObject();

                terminalProps["JAVA_HOME"] = Path.Combine(homePath, "jdk");
                terminalProps["PATH"] = Path.Combine(homePath, "jdk", "bin") + path_seperator + "${env:PATH}";

                settingsJson["terminal.integrated.env." + os] = terminalProps;

            }
            else
            {
                dynamic terminalEnv = settingsJson["terminal.integrated.env." + os];
                terminalEnv["JAVA_HOME"] = Path.Combine(homePath, "jdk");
                string path = terminalEnv["PATH"];
                if (path == null)
                {
                    terminalEnv["PATH"] = Path.Combine(homePath, "jdk", "bin") + path_seperator + "${env:PATH}";
                }
                else
                {
                    var binPath = Path.Combine(homePath, "jdk", "bin");
                    if (!path.Contains(binPath))
                    {
                        path = binPath + path_seperator + path;
                        terminalEnv["PATH"] = path;
                    }
                }
            }

            var serialized = JsonConvert.SerializeObject(settingsJson, Formatting.Indented);
            await File.WriteAllTextAsync(settingsFile, serialized);
        }

        private ValueTask<string> SetVsCodePortableMode()
        {
            string portableFolder = Path.Combine(configurationProvider.InstallDirectory, "vscode");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                portableFolder = Path.Combine(portableFolder, "VSCode-linux-x64", "data");
            }
            else if (OperatingSystem.IsMacOS())
            {
                portableFolder = Path.Combine(portableFolder, "code-portable-data");
            }
            else
            {
                portableFolder = Path.Combine(portableFolder, "data");
            }

            try
            {
                Directory.CreateDirectory(portableFolder);
            }
            catch (IOException)
            {

            }

            return new ValueTask<string>(portableFolder);
        }

        private void SetIfNotSet<T>(string key, T value, dynamic settingsJson)
        {
            if (!settingsJson.ContainsKey(key))
            {

                settingsJson[key] = value;
            }
        }

        private async Task RunVsCodeExtensionsSetup()
        {
            if (!installSelectionModel.InstallVsCodeExtensions) return;

            string codeExe;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                codeExe = Path.Combine(configurationProvider.InstallDirectory, "vscode", "bin", "code.cmd");
            }
            else if (OperatingSystem.IsMacOS())
            {
                codeExe = Path.Combine(configurationProvider.InstallDirectory, "vscode", "Visual Studio Code.app", "Contents", "Resources", "app", "bin", "code");
            }
            else
            {
                codeExe = Path.Combine(configurationProvider.InstallDirectory, "vscode", "VSCode-linux-x64", "bin", "code");
            }

            // Load existing extensions

            var versions = await Task.Run(() =>
            {
                var startInfo = new ProcessStartInfo(codeExe, "--list-extensions --show-versions")
                {
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                var proc = Process.Start(startInfo);
                proc!.WaitForExit();
                var lines = new List<(string name, WPIVersion version)>();
                while (true)
                {
                    string? line = proc.StandardOutput.ReadLine();
                    if (line == null)
                    {
                        return lines;
                    }

                    if (line.Contains('@'))
                    {
                        var split = line.Split('@');
                        lines.Add((split[0], new WPIVersion(split[1])));
                    }
                }
            });

            var availableToInstall = new List<(Extension extension, WPIVersion version, int sortOrder)>();

            availableToInstall.Add((configurationProvider.VsCodeConfig.WPILibExtension,
                new WPIVersion(configurationProvider.VsCodeConfig.WPILibExtension.Version), int.MaxValue));

            for (int i = 0; i < configurationProvider.VsCodeConfig.ThirdPartyExtensions.Length; i++)
            {
                availableToInstall.Add((configurationProvider.VsCodeConfig.ThirdPartyExtensions[i],
                    new WPIVersion(configurationProvider.VsCodeConfig.ThirdPartyExtensions[i].Version), i));
            }

            var maybeUpdates = availableToInstall.Where(x => versions.Select(y => y.name).Contains(x.extension.Name)).ToList();
            var newInstall = availableToInstall.Except(maybeUpdates).ToList();

            var definitelyUpdate = maybeUpdates.Join(versions, x => x.extension.Name, y => y.name,
                (newVersion, existing) => (newVersion, existing))
                .Where(x => x.newVersion.version > x.existing.version).Select(x => x.newVersion);

            var installs = definitelyUpdate.Concat(newInstall)
                                           .OrderBy(x => x.sortOrder)
                                           .Select(x => x.extension)
                                           .ToArray();

            double end = installs.Length;
            foreach (var item in installs)
            {
                var startInfo = new ProcessStartInfo(codeExe, "--install-extension " + Path.Combine(configurationProvider.InstallDirectory, "vsCodeExtensions", item.Vsix))
                {
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                await Task.Run(() =>
                {
                    var proc = Process.Start(startInfo);
                    proc!.WaitForExit();
                });

            }
        }

        private async Task RunShortcutCreator()
        {
            var shortcutData = new ShortcutData();

            var frcHomePath = configurationProvider.InstallDirectory;
            var frcYear = configurationProvider.UpgradeConfig.FrcYear;

            var iconLocation = Path.Join(frcHomePath, configurationProvider.UpgradeConfig.PathFolder, "wpilib-256.ico");
            shortcutData.IsAdmin = installSelectionModel.InstallAsAdmin;

            if (installSelectionModel.InstallVsCode)
            {
                // Add VS Code Shortcuts
                shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "vscode", "Code.exe"), $"{frcYear} WPILib VS Code", $"{frcYear} WPILib VS Code", iconLocation));
                shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "vscode", "Code.exe"), $"Programs/{frcYear} WPILib VS Code", $"{frcYear} WPILib VS Code", iconLocation));
            }

            if (installSelectionModel.InstallTools)
            {
                // Add Tool Shortcuts
                shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "OutlineViewer.vbs"), $"{frcYear} WPILib Tools/OutlineViewer", "OutlineViewer", ""));
                shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "PathWeaver.vbs"), $"{frcYear} WPILib Tools/PathWeaver", "PathWeaver", iconLocation));
                shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "RobotBuilder.vbs"), $"{frcYear} WPILib Tools/RobotBuilder", "RobotBuilder", iconLocation));
                shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "RobotBuilder-Old.vbs"), $"{frcYear} WPILib Tools/RobotBuilder-Old", "RobotBuilder-Old", ""));
                shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "shuffleboard.vbs"), $"{frcYear} WPILib Tools/Shuffleboard", "Shuffleboard", iconLocation));
                shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "SmartDashboard.vbs"), $"{frcYear} WPILib Tools/SmartDashboard", "SmartDashboard", iconLocation));

                shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "OutlineViewer.vbs"), $"Programs/{frcYear} WPILib Tools/OutlineViewer", "OutlineViewer", ""));
                shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "PathWeaver.vbs"), $"Programs/{frcYear} WPILib Tools/PathWeaver", "PathWeaver", iconLocation));
                shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "RobotBuilder.vbs"), $"Programs/{frcYear} WPILib Tools/RobotBuilder", "RobotBuilder", iconLocation));
                shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "RobotBuilder-Old.vbs"), $"Programs/{frcYear} WPILib Tools/RobotBuilder-Old", "RobotBuilder-Old", ""));
                shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "shuffleboard.vbs"), $"Programs/{frcYear} WPILib Tools/Shuffleboard", "Shuffleboard", iconLocation));
                shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "SmartDashboard.vbs"), $"Programs/{frcYear} WPILib Tools/SmartDashboard", "SmartDashboard", iconLocation));
            }

            // Add Documentation Shortcuts
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "documentation", "rtd", "frc-docs-latest", "index.html"), $"{frcYear} WPILib Documentation", $"{frcYear} WPILib Documentation", iconLocation));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "documentation", "rtd", "frc-docs-latest", "index.html"), $"Programs/{frcYear} WPILib Documentation", $"{frcYear} WPILib Documentation", iconLocation));

            var serializedData = JsonConvert.SerializeObject(shortcutData);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Run windows shortcut creater
                var tempFile = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempFile, serializedData);
                var shortcutCreatorPath = Path.Combine(configurationProvider.InstallDirectory, "installUtils", "WPILibShortcutCreator.exe");

                var startInfo = new ProcessStartInfo(shortcutCreatorPath, $"\"{tempFile}\"")
                {
                    WorkingDirectory = Environment.CurrentDirectory
                };
                if (shortcutData.IsAdmin)
                {
                    startInfo.UseShellExecute = true;
                    startInfo.Verb = "runas";
                }
                else
                {
                    startInfo.UseShellExecute = false;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.CreateNoWindow = true;
                    startInfo.RedirectStandardOutput = true;
                }
                var exitCode = await Task.Run(() =>
                {
                    var proc = Process.Start(startInfo);
                    proc!.WaitForExit();
                    return proc.ExitCode;
                });

                if (exitCode != 0)
                {
                    Console.WriteLine("Warning: Shortcut creation failed. Error Code: " + exitCode);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && installSelectionModel.InstallVsCode)
            {
                // Create Linux desktop shortcut
                var desktopFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", $@"FRC VS Code {frcYear}.desktop");
                var launcherFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/applications", $@"FRC VS Code {frcYear}.desktop");
                string contents = $@"#!/usr/bin/env xdg-open
[Desktop Entry]
Version=1.0
Type=Application
Categories=Development
Name=FRC VS Code {frcYear}
Comment=Official C++/Java IDE for the FIRST Robotics Competition
Exec={configurationProvider.InstallDirectory}/frccode/frccode{frcYear}
Icon={configurationProvider.InstallDirectory}/frccode/wpilib-256.ico
Terminal=false
StartupNotify=true
";

                var desktopPath = Path.GetDirectoryName(desktopFile);
                if (desktopPath != null)
                {
                    Directory.CreateDirectory(desktopPath);
                }
                var launcherPath = Path.GetDirectoryName(launcherFile);
                if (launcherPath != null)
                {
                    Directory.CreateDirectory(launcherPath);
                }
                File.WriteAllText(desktopFile, contents);
                File.WriteAllText(launcherFile, contents);
            }
        }
    }
}
