using ReactiveUI;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WPILibInstaller_Avalonia.Interfaces;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class InstallPageViewModel : PageViewModelBase
    {
        private readonly IDependencyInjection di;
        private readonly IToInstallProvider toInstallProvider;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IVsCodeInstallLocationProvider vsInstallProvider;

        private string text = "Starting";
        private int progress = 0;

        public int Progress { get => progress; set => this.RaiseAndSetIfChanged(ref progress, value); }
        public string Text { get => text; set => this.RaiseAndSetIfChanged(ref text, value); }

        public InstallPageViewModel(IScreen screen, IDependencyInjection di, IToInstallProvider toInstallProvider, IConfigurationProvider configurationProvider, IVsCodeInstallLocationProvider vsInstallProvider)
            : base("", "", "Starting", screen)
        {
            this.di = di;
            this.toInstallProvider = toInstallProvider;
            this.configurationProvider = configurationProvider;
            this.vsInstallProvider = vsInstallProvider;
            _ = RunInstall();
        }

        private CancellationTokenSource? source;

        public void CancelInstall()
        {
            source?.Cancel();
        }

        private async Task RunInstall()
        {
            source = new CancellationTokenSource();

            do
            {

                try
                {
                    //await ExtractArchive(source.Token);
                    if (source.IsCancellationRequested) break;
                    //await RunGradleSetup();
                    if (source.IsCancellationRequested) break;
                    //await RunToolSetup();
                    if (source.IsCancellationRequested) break;
                    //await RunCppSetup();
                    if (source.IsCancellationRequested) break;
                    await RunVsCodeSetup(source.Token);
                    if (source.IsCancellationRequested) break;
                    await RunVsCodeExtensionsSetup();
                }
                catch (Exception e)
                {
                    ;
                }

            } while (false);

            if (source.IsCancellationRequested)
            {
                MoveNext(di.Resolve<CanceledPageViewModel>());
            }
            else
            {
                MoveNext();
            }
        }

        public override IObservable<IRoutableViewModel> MoveNext()
        {
            return MoveNext(di.Resolve<FinalPageViewModel>());
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


        private async Task ExtractArchive(CancellationToken token)
        {
            var directoriesToIgnore = GetExtractionIgnoreDirectories();

            var zipArchive = configurationProvider.ZipArchive;

            double totalCount = zipArchive.Entries.Count;
            long currentCount = 0;

            string intoPath = configurationProvider.InstallDirectory;

            foreach (var entry in zipArchive.Entries)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                double currentPercentage = (currentCount / totalCount) * 100;
                currentCount++;
                

                var compressedLength = entry.CompressedLength;

                if (entry.Name == "")
                {
                    continue;
                }

                if (currentPercentage > 100) currentPercentage = 100;
                if (currentPercentage < 0) currentPercentage = 0;
                Progress = (int)currentPercentage;
                Text = "Installing " + entry.FullName;

                var entryName = entry.FullName;
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

                using var stream = entry.Open();
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

        private Task RunGradleSetup()
        {
            if (!toInstallProvider.Model.InstallGradle) return Task.CompletedTask;

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

        }

        private async Task RunToolSetup()
        {
            if (!toInstallProvider.Model.InstallTools || !toInstallProvider.Model.InstallWPILibDeps) return;

            Text = "Configuring Tools";
            Progress = 50;

        }

        private async Task RunVsCodeSetup(CancellationToken token)
        {
            if (!toInstallProvider.Model.InstallVsCode) return;

            Text = "Installing Visual Studio Code";
            Progress = 0;

            var archive = vsInstallProvider.Model.ToExtractArchive!;

            var extractor = archive.ExtractAllEntries();

            double totalSize = archive.TotalUncompressSize;
            long currentSize = 0;


            string intoPath = Path.Join(configurationProvider.InstallDirectory, "vscode");

            while (extractor.MoveToNextEntry())
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                var entry = extractor.Entry;
                currentSize += entry.Size;
                if (entry.IsDirectory) continue;
                Text = "Installing " + entry.Key;

                double currentPercentage = (currentSize / totalSize) * 100;
                if (currentPercentage > 100) currentPercentage = 100;
                if (currentPercentage < 0) currentPercentage = 0;
                Progress = (int)currentPercentage;

                var entryName = entry.Key;

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

        private async Task RunVsCodeExtensionsSetup()
        {
            if (!toInstallProvider.Model.InstallVsCodeExtensions) return;
        }

    }
}
