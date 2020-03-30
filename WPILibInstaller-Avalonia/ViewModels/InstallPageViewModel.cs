using MessageBox.Avalonia;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WPILibInstaller_Avalonia.Interfaces;
using WPILibInstaller_Avalonia.Models;
using WPILibInstaller_Avalonia.Utils;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class InstallPageViewModel : PageViewModelBase
    {
        private readonly IDependencyInjection di;
        private readonly IToInstallProvider toInstallProvider;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IVsCodeInstallLocationProvider vsInstallProvider;
        private readonly IProgramWindow programWindow;

        public int Progress { get; set; }
        public string Text { get; set; } = "";

        public async Task UIUpdateTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                this.RaisePropertyChanged(nameof(Progress));
                this.RaisePropertyChanged(nameof(Text));
                await Task.Delay(100);
            }
        }

        public bool succeeded = false;

        public InstallPageViewModel(IDependencyInjection di, IToInstallProvider toInstallProvider, IConfigurationProvider configurationProvider, IVsCodeInstallLocationProvider vsInstallProvider,
            IProgramWindow programWindow)
            : base("", "")
        {
            this.di = di;
            this.toInstallProvider = toInstallProvider;
            this.configurationProvider = configurationProvider;
            this.vsInstallProvider = vsInstallProvider;
            this.programWindow = programWindow;
            _ = RunInstall();
        }

        private CancellationTokenSource? source;

        public void CancelInstall()
        {
            source?.Cancel();
        }

        private Exception? exp = null;

        private async Task RunInstall()
        {
            source = new CancellationTokenSource();


            var updateSource = new CancellationTokenSource();

            var updateTask = UIUpdateTask(updateSource.Token);

            Stopwatch sw = Stopwatch.StartNew();


            do
            {

                try
                {
                    await ExtractArchive(source.Token);
                    if (source.IsCancellationRequested) break;
                    await RunGradleSetup();
                    if (source.IsCancellationRequested) break;
                    await ConfigureVsCodeSettings();
                    if (source.IsCancellationRequested) break;
                    await RunToolSetup();
                    if (source.IsCancellationRequested) break;
                    await RunCppSetup();
                    if (source.IsCancellationRequested) break;
                    await RunMavenMetaDataFixer();
                    if (source.IsCancellationRequested) break;
                    await RunVsCodeSetup(source.Token);
                    if (source.IsCancellationRequested) break;
                    await RunVsCodeExtensionsSetup();
                }
#pragma warning disable CS0168 // Variable is declared but never used
                catch (Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
                {
                    Debug.WriteLine(e);
                    exp = e;
                }

            } while (false);

            updateSource.Cancel();
            await updateTask;

            if (source.IsCancellationRequested)
            {
                succeeded = false;
            }
            else
            {
                succeeded = true;
            }

            await MessageBoxManager.GetMessageBoxStandardWindow("Time",
                    $"{sw.Elapsed}", icon: MessageBox.Avalonia.Enums.Icon.None).ShowDialog(programWindow.Window);

            di.Resolve<MainWindowViewModel>().GoNext();
        }

        public override PageViewModelBase MoveNext()
        {
            if (exp != null)
            {
                var vm = di.Resolve<CanceledPageViewModel>();
                vm.SetException(exp);
                return vm;
            }

            if (succeeded)
            {
                return di.Resolve<FinalPageViewModel>();
            }
            else
            {
                return di.Resolve<CanceledPageViewModel>();
            }
        }

        private List<string> GetExtractionIgnoreDirectories()
        {
            List<string> ignoreDirs = new List<string>();
            var model = toInstallProvider.Model;
            if (!model.InstallCpp) ignoreDirs.Add(configurationProvider.FullConfig.CppToolchain.Directory + "/");
            if (!model.InstallGradle) ignoreDirs.Add(configurationProvider.FullConfig.Gradle.ZipName);
            if (!model.InstallJDK) ignoreDirs.Add(configurationProvider.JdkConfig.Folder + "/");
            if (!model.InstallTools) ignoreDirs.Add(configurationProvider.UpgradeConfig.Tools.Folder + "/");
            if (!model.InstallWPILibDeps) ignoreDirs.Add(configurationProvider.UpgradeConfig.Maven.Folder + "/");

            return ignoreDirs;
        }

        private void SetIfNotSet<T>(string key, T value, dynamic settingsJson)
        {
            if (!settingsJson.ContainsKey(key))
            {

                settingsJson[key] = value;
            }
        }

        private async Task ConfigureVsCodeSettings()
        {


            var homePath = configurationProvider.InstallDirectory;
            var vscodePath = Path.Combine(homePath, "vscode");
            var settingsDir = Path.Combine(vscodePath, "data", "user-data", "User");
            var settingsFile = Path.Combine(settingsDir, "settings.json");

            var dataFolder = Path.Combine(vscodePath, "data");
            try
            {
                Directory.CreateDirectory(dataFolder);
            }
            catch (IOException)
            {

            }

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

            if (!settingsJson.ContainsKey("terminal.integrated.env.windows"))
            {
                dynamic terminalProps = new JObject();

                terminalProps["JAVA_HOME"] = Path.Combine(homePath, "jdk");
                terminalProps["PATH"] = Path.Combine(homePath, "jdk", "bin") + ":${env:PATH}";

                settingsJson["terminal.integrated.env.windows"] = terminalProps;

            }
            else
            {
                dynamic terminalEnv = settingsJson["terminal.integrated.env.windows"];
                terminalEnv["JAVA_HOME"] = Path.Combine(homePath, "jdk");
                string path = terminalEnv["PATH"];
                if (path == null)
                {
                    terminalEnv["PATH"] = Path.Combine(homePath, "jdk", "bin") + ";${env:PATH}";
                }
                else
                {
                    var binPath = Path.Combine(homePath, "jdk", "bin");
                    if (!path.Contains(binPath))
                    {
                        path = binPath + ";" + path;
                        terminalEnv["PATH"] = path;
                    }
                }
            }

            var serialized = JsonConvert.SerializeObject(settingsJson, Formatting.Indented);
            await File.WriteAllTextAsync(settingsFile, serialized);
        }

        private async Task RunVsCodeSetup(CancellationToken token)
        {
            if (!toInstallProvider.Model.InstallVsCode) return;

            Text = "Installing Visual Studio Code";
            Progress = 0;

            var archive = vsInstallProvider.Model.ToExtractArchive!;

            var extractor = archive;

            double totalSize = archive.TotalUncompressSize;
            long currentSize = 0;


            string intoPath = Path.Join(configurationProvider.InstallDirectory, "vscode");

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



                using var stream = extractor.OpenEntryStream();
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

                using FileStream writer = File.Create(fullZipToPath);
                await stream.CopyToAsync(writer);
            }

        }

        private async Task ExtractArchive(CancellationToken token)
        {
            var directoriesToIgnore = GetExtractionIgnoreDirectories();

            Progress = 0;

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
                bool skip = false;
                foreach (var ignore in directoriesToIgnore)
                {
                    if (entryName.StartsWith(ignore))
                    {
                        skip = true;
                        break;
                    }
                }

                if (skip)
                {
                    continue;
                }

                Text = "Installing " + entryName;

                double currentPercentage = (currentSize / totalSize) * 100;
                if (currentPercentage > 100) currentPercentage = 100;
                if (currentPercentage < 0) currentPercentage = 0;
                Progress = (int)currentPercentage;

                using var stream = extractor.OpenEntryStream();
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

                using FileStream writer = File.Create(fullZipToPath);
                await stream.CopyToAsync(writer);
            }

            ;
        }

        private Task RunGradleSetup()
        {
            if (!toInstallProvider.Model.InstallGradle || !toInstallProvider.Model.InstallWPILibDeps) return Task.CompletedTask;

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
            if (!toInstallProvider.Model.InstallCpp) return;

            Text = "Configuring C++";
            Progress = 50;

            await Task.Yield();
        }

        private Task<bool> RunScriptExecutable(string script, params string[] args)
        {
            ProcessStartInfo pstart = new ProcessStartInfo(script, string.Join(" ", args));
            var p = Process.Start(pstart);
            return Task.Run(() =>
            {
                return p.WaitForExit(5000);
            });
        }

        private async Task RunToolSetup()
        {
            if (!toInstallProvider.Model.InstallTools || !toInstallProvider.Model.InstallWPILibDeps)
                return;

            Text = "Configuring Tools";
            Progress = 50;

            var didComplete = await RunScriptExecutable(Path.Combine(configurationProvider.InstallDirectory,
                configurationProvider.UpgradeConfig.Tools.Folder,
                configurationProvider.UpgradeConfig.Tools.UpdaterExe), "silent");
        }

        private async Task RunMavenMetaDataFixer()
        {
            if (!toInstallProvider.Model.InstallWPILibDeps)
                return;

            Text = "Fixing up maven metadata";
            Progress = 50;

            var didComplete = await RunScriptExecutable(Path.Combine(configurationProvider.InstallDirectory,
                configurationProvider.UpgradeConfig.Maven.Folder,
                configurationProvider.UpgradeConfig.Maven.MetaDataFixerExe), "silent");
        }



        private async Task RunVsCodeExtensionsSetup()
        {
            if (!toInstallProvider.Model.InstallVsCodeExtensions) return;

            string codeExe;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                codeExe = Path.Combine(configurationProvider.InstallDirectory, "vscode", "bin", "code.cmd");
            }
            else
            {
                return;
            }

            // Load existing extensions

            var versions = await Task.Run(() =>
            {
                var startInfo = new ProcessStartInfo(codeExe, "--list-extensions --show-versions");
                startInfo.UseShellExecute = false;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardOutput = true;
                var proc = Process.Start(startInfo);
                proc.WaitForExit();
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

            Text = "Installing Extensions";


            int idx = 0;
            double end = installs.Length;
            Progress = 0;
            foreach (var item in installs)
            {
                var startInfo = new ProcessStartInfo(codeExe, "--install-extension " + Path.Combine(configurationProvider.InstallDirectory, "vsCodeExtensions", item.Vsix));
                startInfo.UseShellExecute = false;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardOutput = true;
                await Task.Run(() =>
                {
                    var proc = Process.Start(startInfo);
                    proc.WaitForExit();
                });

                idx++;

                double percentage = (idx / end) * 100;
                if (percentage > 100) percentage = 100;
                if (percentage < 0) percentage = 0;
                Progress = (int)percentage;


            }
        }

    }
}
