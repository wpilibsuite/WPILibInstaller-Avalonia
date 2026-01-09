using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.Services
{
    public class VsCodeInstallationService : IVsCodeInstallationService
    {
        private readonly IConfigurationProvider configurationProvider;
        private readonly IVsCodeInstallLocationProvider vsInstallProvider;

        public VsCodeInstallationService(
            IConfigurationProvider configurationProvider,
            IVsCodeInstallLocationProvider vsInstallProvider)
        {
            this.configurationProvider = configurationProvider;
            this.vsInstallProvider = vsInstallProvider;
        }

        private ValueTask<string> SetVsCodePortableMode()
        {
            string portableFolder = Path.Combine(configurationProvider.InstallDirectory, "vscode");

            var currentPlatform = PlatformUtils.CurrentPlatform;
            switch (currentPlatform)
            {
                case Platform.Win64:
                    portableFolder = Path.Combine(portableFolder, "data");
                    break;
                case Platform.MacArm64:
                case Platform.Mac64:
                    portableFolder = Path.Combine(portableFolder, "code-portable-data");
                    break;
                case Platform.Linux64:
                    portableFolder = Path.Combine(portableFolder, "VSCode-linux-x64", "data");
                    break;
                case Platform.LinuxArm64:
                    portableFolder = Path.Combine(portableFolder, "VSCode-linux-arm64", "data");
                    break;
                default:
                    throw new PlatformNotSupportedException("Invalid platform");
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

        public async Task ConfigureVsCodeSettings()
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

            VsCodeSettingsUtils.SetIfNotSet("java.jdt.ls.java.home", Path.Combine(homePath, "jdk"), settingsJson);
            VsCodeSettingsUtils.SetIfNotSetIgnoreSync("extensions.autoUpdate", false, settingsJson);
            VsCodeSettingsUtils.SetIfNotSetIgnoreSync("extensions.autoCheckUpdates", false, settingsJson);
            VsCodeSettingsUtils.SetIfNotSetIgnoreSync("extensions.ignoreRecommendations", true, settingsJson);
            VsCodeSettingsUtils.SetIfNotSetIgnoreSync("extensions.showRecommendationsOnlyOnDemand", true, settingsJson);
            VsCodeSettingsUtils.SetIfNotSetIgnoreSync("update.mode", "none", settingsJson);
            VsCodeSettingsUtils.SetIfNotSetIgnoreSync("update.showReleaseNotes", false, settingsJson);
            VsCodeSettingsUtils.SetIfNotSetIgnoreSync("java.completion.matchCase", "off", settingsJson);
            VsCodeSettingsUtils.SetIfNotSetIgnoreSync("workbench.secondarySideBar.defaultVisibility", "hidden", settingsJson);

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
                    if (!path.ToString().Contains(binPath))
                    {
                        path = binPath + path_seperator + path;
                        terminalEnv["PATH"] = path;
                    }
                }
            }
            VsCodeSettingsUtils.IgnoreSync("terminal.integrated.env." + os, settingsJson);

            if (settingsJson.ContainsKey("java.configuration.runtimes"))
            {
                JArray javaConfigEnv = (JArray)settingsJson["java.configuration.runtimes"]!;
                Boolean javaFound = false;
                foreach (JToken result in javaConfigEnv)
                {
                    JToken? name = result["name"];
                    if (name != null)
                    {
                        if (name.ToString().Equals("JavaSE-17"))
                        {
                            result["path"] = Path.Combine(homePath, "jdk");
                            result["default"] = true;
                            javaFound = true;
                        }
                        else
                        {
                            result["default"] = false;
                        }
                    }
                }
                if (!javaFound)
                {
                    JObject javaConfigProp = new JObject
                    {
                        ["name"] = "JavaSE-17",
                        ["path"] = Path.Combine(homePath, "jdk"),
                        ["default"] = true
                    };
                    javaConfigEnv.Add(javaConfigProp);
                    settingsJson["java.configuration.runtimes"] = javaConfigEnv;
                }
            }
            else
            {
                JArray javaConfigProps = new JArray();
                JObject javaConfigProp = new JObject
                {
                    ["name"] = "JavaSE-17",
                    ["path"] = Path.Combine(homePath, "jdk"),
                    ["default"] = true
                };
                javaConfigProps.Add(javaConfigProp);
                settingsJson["java.configuration.runtimes"] = javaConfigProps;
            }

            if (settingsJson.ContainsKey("settingsSync.ignoredExtensions"))
            {
                JArray ignoredExtensions = (JArray)settingsJson["settingsSync.ignoredExtensions"]!;
                Boolean keyFound = false;
                foreach (JToken result in ignoredExtensions)
                {
                    if (result.Value<string>() != null)
                    {
                        if (result.Value<string>() == "wpilibsuite.vscode-wpilib")
                        {
                            keyFound = true;
                        }
                    }
                }
                if (!keyFound)
                {
                    ignoredExtensions.Add("wpilibsuite.vscode-wpilib");
                    settingsJson["settingsSync.ignoredExtensions"] = ignoredExtensions;
                }
            }
            else
            {
                JArray ignoredExtensions = ["wpilibsuite.vscode-wpilib"];
                settingsJson["settingsSync.ignoredExtensions"] = ignoredExtensions;
            }

            var serialized = JsonConvert.SerializeObject(settingsJson, Formatting.Indented);
            await File.WriteAllTextAsync(settingsFile, serialized);
        }

        public async Task RunVsCodeSetup(CancellationToken token, IProgress<InstallProgress>? progress = null)
        {
            if (!vsInstallProvider.Model.InstallingVsCode) return;

            progress?.Report(new InstallProgress(0, "Installing Visual Studio Code"));

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
                await ProcessExecutionUtils.RunScriptExecutable("unzip", Timeout.Infinite, token, zipPath, "-d", intoPath);
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

                double currentPercentage = (currentSize / totalSize) * 100;
                if (currentPercentage > 100) currentPercentage = 100;
                if (currentPercentage < 0) currentPercentage = 0;

                progress?.Report(new InstallProgress((int)currentPercentage, "Installing " + entryName));

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

        public async Task RunVsCodeExtensionsSetup(IProgress<InstallProgress>? progress = null)
        {
            if (!vsInstallProvider.Model.InstallExtensions) return;

            string codeExe;

            var currentPlatform = PlatformUtils.CurrentPlatform;
            switch (currentPlatform)
            {
                case Platform.Win64:
                    codeExe = Path.Combine(configurationProvider.InstallDirectory, "vscode", "bin", "code.cmd");
                    break;
                case Platform.MacArm64:
                case Platform.Mac64:
                    var appDirectories = Directory.GetDirectories(Path.Combine(configurationProvider.InstallDirectory, "vscode"), "*.app");
                    if (appDirectories.Length != 1)
                    {
                        throw new InvalidOperationException("Expected exactly one .app directory in the vscode folder.");
                    }
                    codeExe = Path.Combine(appDirectories[0], "Contents", "Resources", "app", "bin", "code");
                    break;
                case Platform.Linux64:
                    codeExe = Path.Combine(configurationProvider.InstallDirectory, "vscode", "VSCode-linux-x64", "bin", "code");
                    break;
                case Platform.LinuxArm64:
                    codeExe = Path.Combine(configurationProvider.InstallDirectory, "vscode", "VSCode-linux-arm64", "bin", "code");
                    break;
                default:
                    throw new PlatformNotSupportedException("Invalid platform");
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

            progress?.Report(new InstallProgress(0, "Installing Extensions"));


            int idx = 0;
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

                idx++;

                double percentage = (idx / end) * 100;
                if (percentage > 100) percentage = 100;
                if (percentage < 0) percentage = 0;

                progress?.Report(new InstallProgress((int)percentage, "Installing Extension " + item.Name));
            }
        }
    }
}
