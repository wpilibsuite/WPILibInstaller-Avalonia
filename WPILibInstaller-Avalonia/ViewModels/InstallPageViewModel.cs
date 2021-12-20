using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.ViewModels
{
    public class InstallPageViewModel : PageViewModelBase
    {
        private readonly IViewModelResolver viewModelResolver;
        private readonly IToInstallProvider toInstallProvider;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IVsCodeInstallLocationProvider vsInstallProvider;
        private readonly IProgramWindow programWindow;

        public int Progress { get; set; }
        public string Text { get; set; } = "";
        public int ProgressTotal { get; set; }
        public string TextTotal { get; set; } = "";

        public async Task UIUpdateTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                this.RaisePropertyChanged(nameof(Progress));
                this.RaisePropertyChanged(nameof(Text));
                this.RaisePropertyChanged(nameof(ProgressTotal));
                this.RaisePropertyChanged(nameof(TextTotal));
                try
                {
                    await Task.Delay(100, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public bool succeeded = false;

        private readonly Task runInstallTask;

        public InstallPageViewModel(IViewModelResolver viewModelResolver, IToInstallProvider toInstallProvider, IConfigurationProvider configurationProvider, IVsCodeInstallLocationProvider vsInstallProvider,
            IProgramWindow programWindow, ICatchableButtonFactory buttonFactory)
            : base("", "")
        {
            this.viewModelResolver = viewModelResolver;
            this.toInstallProvider = toInstallProvider;
            this.configurationProvider = configurationProvider;
            this.vsInstallProvider = vsInstallProvider;
            this.programWindow = programWindow;
            CancelInstall = buttonFactory.CreateCatchableButton(CancelInstallFunc);
            runInstallTask = installFunc();

            async Task installFunc()
            {
                try
                {
                    await RunInstall();
                }
                catch (Exception e)
                {
                    viewModelResolver.ResolveMainWindow().HandleException(e);
                }
            }
        }

        private CancellationTokenSource? source;

        public ReactiveCommand<Unit, Unit> CancelInstall { get; }

        public async Task CancelInstallFunc()
        {
            source?.Cancel();
            await runInstallTask;
        }

        private async Task ExtractJDKAndTools(CancellationToken token)
        {
            await ExtractArchive(token, new[] {
                configurationProvider.JdkConfig.Folder + "/",
                configurationProvider.UpgradeConfig.Tools.Folder + "/",
                "installUtils/"});
        }

        private async Task InstallTools(CancellationToken token)
        {
            try
            {
                do
                {
                    ProgressTotal = 0;
                    TextTotal = "Extracting JDK and Tools";
                    await ExtractJDKAndTools(token);
                    if (token.IsCancellationRequested) break;
                    ProgressTotal = 33;
                    TextTotal = "Installing Tools";
                    await RunToolSetup();
                    ProgressTotal = 66;
                    TextTotal = "Creating Shortcuts";
                    await RunShortcutCreator(token);
                } while (false);
            }
            catch (OperationCanceledException)
            {
                // Do nothing, we just want to ignore
            }
        }

        private async Task InstallEverything(CancellationToken token)
        {
            try
            {
                do
                {
                    ProgressTotal = 0;
                    TextTotal = "Extracting";
                    await ExtractArchive(token, null);
                    if (token.IsCancellationRequested) break;
                    ProgressTotal = 11;
                    TextTotal = "Installing Gradle";
                    await RunGradleSetup();
                    if (token.IsCancellationRequested) break;
                    ProgressTotal = 22;
                    TextTotal = "Installing Tools";
                    await RunToolSetup();
                    if (token.IsCancellationRequested) break;
                    ProgressTotal = 33;
                    TextTotal = "Installing CPP";
                    await RunCppSetup();
                    if (token.IsCancellationRequested) break;
                    ProgressTotal = 44;
                    TextTotal = "Fixing Maven";
                    await RunMavenMetaDataFixer();
                    if (token.IsCancellationRequested) break;
                    ProgressTotal = 55;
                    TextTotal = "Installing VS Code";
                    await RunVsCodeSetup(token);
                    if (token.IsCancellationRequested) break;
                    ProgressTotal = 66;
                    TextTotal = "Configuring VS Code";
                    await ConfigureVsCodeSettings();
                    if (token.IsCancellationRequested) break;
                    ProgressTotal = 77;
                    TextTotal = "Installing VS Code Extensions";
                    await RunVsCodeExtensionsSetup();
                    if (token.IsCancellationRequested) break;
                    ProgressTotal = 88;
                    TextTotal = "Creating Shortcuts";
                    await RunShortcutCreator(token);
                } while (false);
            }
            catch (OperationCanceledException)
            {
                // Do nothing, we just want to ignore
            }
        }

        private async Task RunInstall()
        {
            source = new CancellationTokenSource();

            await Task.Yield();

            var updateSource = new CancellationTokenSource();

            var updateTask = UIUpdateTask(updateSource.Token);

            try
            {
                if (toInstallProvider.Model.InstallTools)
                {
                    await InstallTools(source.Token);
                }
                else
                {
                    await InstallEverything(source.Token);
                }

                updateSource.Cancel();
                await updateTask;
            }
            catch (OperationCanceledException)
            {
                // Ignore, as we just want to continue
            }

            if (source.IsCancellationRequested)
            {
                succeeded = false;
            }
            else
            {
                succeeded = true;
            }

            await viewModelResolver.ResolveMainWindow().ExecuteGoNext();
        }

        public override PageViewModelBase MoveNext()
        {
            if (succeeded)
            {
                return viewModelResolver.Resolve<FinalPageViewModel>();
            }
            else
            {
                return viewModelResolver.Resolve<CanceledPageViewModel>();
            }
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

        private static void SetIfNotSet(string key, object value, JObject settingsJson)
        {
            if (!settingsJson.ContainsKey(key))
            {
                settingsJson[key] = JToken.FromObject(value);
            }
        }

        private async Task ConfigureVsCodeSettings()
        {
            if (!vsInstallProvider.Model.InstallExtensions) return;

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

            JObject settingsJson = new JObject();
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
                JObject terminalProps = new JObject
                {
                    ["JAVA_HOME"] = Path.Combine(homePath, "jdk"),
                    ["PATH"] = Path.Combine(homePath, "jdk", "bin") + path_seperator + "${env:PATH}"
                };

                settingsJson["terminal.integrated.env." + os] = terminalProps;

            }
            else
            {
                JToken terminalEnv = settingsJson["terminal.integrated.env." + os]!;
                terminalEnv["JAVA_HOME"] = Path.Combine(homePath, "jdk");
                JToken? path = terminalEnv["PATH"];
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

        private async Task RunVsCodeSetup(CancellationToken token)
        {
            if (!vsInstallProvider.Model.InstallingVsCode) return;

            Text = "Installing Visual Studio Code";
            Progress = 0;

            string intoPath = Path.Join(configurationProvider.InstallDirectory, "vscode");

            if (vsInstallProvider.Model.ToExtractArchiveMacOs != null)
            {
                vsInstallProvider.Model.ToExtractArchiveMacOs.Seek(0, SeekOrigin.Begin);
                var zipPath = Path.Join(intoPath, "MacVsCode.zip");
                Directory.CreateDirectory(intoPath);
                {
                    using var fileToWrite = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await vsInstallProvider.Model.ToExtractArchiveMacOs.CopyToAsync(fileToWrite, token);
                }
                await RunScriptExecutable("unzip", Timeout.Infinite, zipPath, "-d", intoPath);
                File.Delete(zipPath);
                return;
            }

            var archive = vsInstallProvider.Model.ToExtractArchive!;

            var extractor = archive;

            double totalSize = archive.TotalUncompressSize;
            long currentSize = 0;

            while (extractor.MoveToNextEntry())
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                currentSize += extractor.EntrySize;
                if (extractor.EntryIsDirectory) continue;
                var entryName = extractor.EntryKey;
                Text = "Installing " + entryName;

                double currentPercentage = (currentSize / totalSize) * 100;
                if (currentPercentage > 100) currentPercentage = 100;
                if (currentPercentage < 0) currentPercentage = 0;
                Progress = (int)currentPercentage;

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

                if (extractor.EntryIsExecutable)
                {
                    new Mono.Unix.UnixFileInfo(fullZipToPath).FileAccessPermissions |=
                        (Mono.Unix.FileAccessPermissions.GroupExecute |
                         Mono.Unix.FileAccessPermissions.UserExecute |
                         Mono.Unix.FileAccessPermissions.OtherExecute);
                }
            }

        }

        private async Task ExtractArchive(CancellationToken token, string[]? filter)
        {
            Progress = 0;
            if (OperatingSystem.IsWindows())
            {
                Text = "Checking for currently running JDKs";
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
                    await MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(new MessageBox.Avalonia.DTO.MessageBoxStandardParams
                    {
                        ContentTitle = "JDKs Running",
                        ContentMessage = msg,
                        Icon = MessageBox.Avalonia.Enums.Icon.Error,
                        ButtonDefinitions = MessageBox.Avalonia.Enums.ButtonEnum.Ok
                    }).ShowDialog(programWindow.Window);
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
                if (token.IsCancellationRequested)
                {
                    return;
                }
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


                Text = "Installing " + entryName;

                double currentPercentage = (currentSize / totalSize) * 100;
                if (currentPercentage > 100) currentPercentage = 100;
                if (currentPercentage < 0) currentPercentage = 0;
                Progress = (int)currentPercentage;

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

                if (extractor.EntryIsExecutable)
                {
                    new Mono.Unix.UnixFileInfo(fullZipToPath).FileAccessPermissions |=
                        (Mono.Unix.FileAccessPermissions.GroupExecute |
                         Mono.Unix.FileAccessPermissions.UserExecute |
                         Mono.Unix.FileAccessPermissions.OtherExecute);
                }
            }
        }

        private Task RunGradleSetup()
        {
            Text = "Configuring Gradle";
            Progress = 50;

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

        private async Task RunCppSetup()
        {
            Text = "Configuring C++";
            Progress = 50;

            await Task.Yield();
        }

        private static Task<bool> RunScriptExecutable(string script, int timeoutMs, params string[] args)
        {
            ProcessStartInfo pstart = new ProcessStartInfo(script, string.Join(" ", args));
            var p = Process.Start(pstart);
            return Task.Run(() =>
            {
                return p!.WaitForExit(timeoutMs);
            });
        }

        private async Task RunToolSetup()
        {
            Text = "Configuring Tools";
            Progress = 50;

            await RunScriptExecutable(Path.Combine(configurationProvider.InstallDirectory,
                configurationProvider.UpgradeConfig.Tools.Folder,
                configurationProvider.UpgradeConfig.Tools.UpdaterExe), 5000, "silent");
        }

        private async Task RunMavenMetaDataFixer()
        {
            Text = "Fixing up maven metadata";
            Progress = 50;

            await RunScriptExecutable(Path.Combine(configurationProvider.InstallDirectory,
                configurationProvider.UpgradeConfig.Maven.Folder,
                configurationProvider.UpgradeConfig.Maven.MetaDataFixerExe), 5000, "silent");
        }



        private async Task RunVsCodeExtensionsSetup()
        {
            if (!vsInstallProvider.Model.InstallExtensions) return;

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

            var availableToInstall = new List<(Extension extension, WPIVersion version, int sortOrder)>
            {
                (configurationProvider.VsCodeConfig.WPILibExtension,
                new WPIVersion(configurationProvider.VsCodeConfig.WPILibExtension.Version), int.MaxValue)
            };

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

            Text = "Installing Extensions";


            int idx = 0;
            double end = installs.Length;
            Progress = 0;
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

                idx++;

                double percentage = (idx / end) * 100;
                if (percentage > 100) percentage = 100;
                if (percentage < 0) percentage = 0;
                Progress = (int)percentage;


            }
        }

        private async Task RunShortcutCreator(CancellationToken token)
        {
            var shortcutData = new ShortcutData();

            var frcHomePath = configurationProvider.InstallDirectory;
            var frcYear = configurationProvider.UpgradeConfig.FrcYear;

            shortcutData.IconLocation = Path.Join(frcHomePath, configurationProvider.UpgradeConfig.PathFolder, "wpilib-256.ico");
            shortcutData.IsAdmin = toInstallProvider.Model.InstallAsAdmin;

            if (vsInstallProvider.Model.InstallingVsCode)
            {
                // Add VS Code Shortcuts
                shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "vscode", "Code.exe"), $"{frcYear} WPILib VS Code", $"{frcYear} WPILib VS Code"));
                shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "vscode", "Code.exe"), $"Programs/{frcYear} WPILib VS Code", $"{frcYear} WPILib VS Code"));
            }

            // Add Tool Shortcuts
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "Glass.vbs"), $"{frcYear} WPILib Tools/Glass {frcYear}", $"Glass {frcYear}"));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "OutlineViewer.vbs"), $"{frcYear} WPILib Tools/OutlineViewer {frcYear}", $"OutlineViewer {frcYear}"));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "PathWeaver.vbs"), $"{frcYear} WPILib Tools/PathWeaver {frcYear}", $"PathWeaver {frcYear}"));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "RobotBuilder.vbs"), $"{frcYear} WPILib Tools/RobotBuilder {frcYear}", $"RobotBuilder {frcYear}"));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "RobotBuilder-Old.vbs"), $"{frcYear} WPILib Tools/RobotBuilder-Old {frcYear}", $"RobotBuilder for Old Command Framework {frcYear}"));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "shuffleboard.vbs"), $"{frcYear} WPILib Tools/Shuffleboard {frcYear}", $"Shuffleboard {frcYear}"));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "SmartDashboard.vbs"), $"{frcYear} WPILib Tools/SmartDashboard {frcYear}", $"SmartDashboard {frcYear}"));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "SysId.vbs"), $"{frcYear} WPILib Tools/SysId {frcYear}", $"SysId {frcYear}"));
            shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "roboRIOTeamNumberSetter.vbs"), $"{frcYear} WPILib Tools/roboRIO Team Number Setter {frcYear}", $"roboRIO Team Number Setter {frcYear}"));

            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "Glass.vbs"), $"Programs/{frcYear} WPILib Tools/Glass {frcYear}", $"Glass {frcYear}"));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "OutlineViewer.vbs"), $"Programs/{frcYear} WPILib Tools/OutlineViewer {frcYear}", $"OutlineViewer {frcYear}"));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "PathWeaver.vbs"), $"Programs/{frcYear} WPILib Tools/PathWeaver {frcYear}", $"PathWeaver {frcYear}"));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "RobotBuilder.vbs"), $"Programs/{frcYear} WPILib Tools/RobotBuilder {frcYear}", $"RobotBuilder {frcYear}"));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "RobotBuilder-Old.vbs"), $"Programs/{frcYear} WPILib Tools/RobotBuilder-Old {frcYear}", $"RobotBuilder for Old Command Framework {frcYear}"));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "shuffleboard.vbs"), $"Programs/{frcYear} WPILib Tools/Shuffleboard {frcYear}", $"Shuffleboard {frcYear}"));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "SmartDashboard.vbs"), $"Programs/{frcYear} WPILib Tools/SmartDashboard {frcYear}", $"SmartDashboard {frcYear}"));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "SysId.vbs"), $"Programs/{frcYear} WPILib Tools/SysId {frcYear}", $"SysId {frcYear}"));
            shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "tools", "roboRIOTeamNumberSetter.vbs"), $"Programs/{frcYear} WPILib Tools/roboRIO Team Number Setter {frcYear}", $"roboRIO Team Number Setter {frcYear}"));

            if (toInstallProvider.Model.InstallEverything)
            {
                // Add Documentation Shortcuts
                shortcutData.DesktopShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "documentation", "rtd", "frc-docs-latest", "index.html"), $"{frcYear} WPILib Documentation", $"{frcYear} WPILib Documentation"));
                shortcutData.StartMenuShortcuts.Add(new ShortcutInfo(Path.Join(frcHomePath, "documentation", "rtd", "frc-docs-latest", "index.html"), $"Programs/{frcYear} WPILib Documentation", $"{frcYear} WPILib Documentation"));
            }

            var serializedData = JsonConvert.SerializeObject(shortcutData);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Run windows shortcut creater
                var tempFile = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempFile, serializedData, token);
                var shortcutCreatorPath = Path.Combine(configurationProvider.InstallDirectory, "installUtils", "WPILibShortcutCreator.exe");

                do
                {
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
                        try
                        {
                            var proc = Process.Start(startInfo);
                            proc!.WaitForExit();
                            return proc.ExitCode;
                        }
                        catch (Win32Exception ex)
                        {
                            return ex.NativeErrorCode;
                        }
                    });

                    if (exitCode == 1223) // ERROR_CANCLED
                    {
                        var results = await MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(new MessageBox.Avalonia.DTO.MessageBoxStandardParams
                        {
                            ContentTitle = "UAC Prompt Cancelled",
                            ContentMessage = "UAC Prompt Cancelled or Timed Out. Would you like to retry?",
                            Icon = MessageBox.Avalonia.Enums.Icon.Info,
                            ButtonDefinitions = MessageBox.Avalonia.Enums.ButtonEnum.YesNo
                        }).ShowDialog(programWindow.Window);
                        if (results == MessageBox.Avalonia.Enums.ButtonResult.Yes)
                        {
                            continue;
                        }
                        break;
                    }

                    if (exitCode != 0)
                    {
                        await MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(new MessageBox.Avalonia.DTO.MessageBoxStandardParams
                        {
                            ContentTitle = "Shortcut Creation Failed",
                            ContentMessage = $"Shortcut creation failed with error code {exitCode}",
                            Icon = MessageBox.Avalonia.Enums.Icon.Warning,
                            ButtonDefinitions = MessageBox.Avalonia.Enums.ButtonEnum.Ok
                        }).ShowDialog(programWindow.Window);
                        break;
                    }
                    break;
                } while (true);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && vsInstallProvider.Model.InstallingVsCode)
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
                await File.WriteAllTextAsync(desktopFile, contents, token);
                await File.WriteAllTextAsync(launcherFile, contents, token);
                await Task.Run(() =>
                {
                    var startInfo = new ProcessStartInfo("chmod", $"+x \"{desktopFile}\"")
                    {
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    };
                    var proc = Process.Start(startInfo);
                    proc!.WaitForExit();
                }, token);
            }
        }
    }
}
