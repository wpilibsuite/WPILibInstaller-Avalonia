using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using Spectre.Console;

using WPILibInstaller.InstallTasks;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Interfaces.Observer;
using WPILibInstaller.Models.CLI;
using WPILibInstaller.Utils;

using static WPILibInstaller.Utils.ArchiveUtils;

namespace WPILibInstaller.CLI
{
    public class Installer : IObserver
    {
        private readonly IConfigurationProvider configurationProvider;
        private readonly CLIInstallSelectionModel installSelectionModel;

        private Func<Task<bool>> uacTimeoutCallback;

        public int Progress { get; set; }
        public string Text { get; set; } = "";

        private ProgressTask? currentSpectreTask;

        public Installer(string[] args)
        {
            var parser = new Parser(args);
            configurationProvider = parser.configurationProvider;
            installSelectionModel = parser.installSelectionModel;
            uacTimeoutCallback = Installer.getUacTimeoutCallback();
        }

        public void Update(ISubject subject)
        {
            if ((subject as InstallTask) != null)
            {
                InstallTask task = (subject as InstallTask)!;
                Progress = task.Progress;
                Text = task.Text;

                if (currentSpectreTask != null)
                    currentSpectreTask.Value = Progress;
            }
        }


        public async Task Install()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn(),
            })
            .StartAsync(async progress =>
            {
                // helper
                async Task RunWithBar(string status, InstallTask task, CancellationToken token)
                {
                    var bar = progress.AddTask(status, maxValue: 100);
                    currentSpectreTask = bar;
                    bar.Description(status);

                    task.Attach(this);
                    try
                    {
                        await task.Execute(token);
                    }
                    finally
                    {
                        task.Detach(this);
                        bar.Value = 100;
                        bar.StopTask();
                    }
                }

                //
                // Extract Archive
                //
                try
                {
                    await RunWithBar(
                        "Extracting archive",
                        new ExtractArchiveTask(configurationProvider, null),
                        token
                    );
                }
                catch (FoundRunningExeException)
                {
                    foundRunningExeHandler();
                }

                //
                // Gradle
                //
                if (installSelectionModel.InstallGradle)
                {
                    await RunWithBar(
                        "Installing Gradle",
                        new GradleSetupTask(configurationProvider),
                        token
                    );
                }

                //
                // Tools
                //
                if (installSelectionModel.InstallTools)
                {
                    await RunWithBar(
                        "Installing Tools",
                        new ToolSetupTask(configurationProvider),
                        token
                    );
                }

                //
                // C++
                //
                if (installSelectionModel.InstallCpp)
                {
                    await RunWithBar(
                        "Installing C++",
                        new CppSetupTask(configurationProvider),
                        token
                    );
                }

                //
                // Maven Metadata
                //
                await RunWithBar(
                    "Fixing Maven metadata",
                    new MavenMetaDataFixerTask(configurationProvider),
                    token
                );

                //
                // VS Code
                //
                if (installSelectionModel.InstallVsCode)
                {
                    var status = "Downloading VS Code";
                    var bar = progress.AddTask(status, maxValue: 100);
                    currentSpectreTask = bar;
                    bar.Description(status);
                    await DownloadVsCode();

                    await RunWithBar(
                        "Extracting VS Code",
                        new VsCodeSetupTask(configurationProvider.VsCodeModel, configurationProvider),
                        token
                    );

                    await RunWithBar(
                        "Configuring VS Code",
                        new ConfigureVsCodeSettingsTask(configurationProvider.VsCodeModel, configurationProvider),
                        token
                    );

                    await RunWithBar(
                        "Installing VS Code Extensions",
                        new VsCodeExtensionsSetupTask(configurationProvider.VsCodeModel, configurationProvider),
                        token
                    );
                }

                //
                // Shortcuts
                //
                var shortcutTask = new ShortcutCreatorTask(
                    configurationProvider.VsCodeModel,
                    configurationProvider,
                    installSelectionModel.InstallAsAdmin,
                    installSelectionModel.InstallDocs
                );
                shortcutTask.uacTimeoutCallback = uacTimeoutCallback;

                try
                {
                    await RunWithBar(
                        "Creating Shortcuts",
                        shortcutTask,
                        token
                    );
                }
                catch (ShortcutCreationFailedException)
                {
                    throw;
                }

            });

            Spectre.Console.AnsiConsole.MarkupLine("Installation complete!");
        }

#pragma warning disable CS1998
        private static Func<Task<bool>> getUacTimeoutCallback()
        {
            return async () =>
            {
                return false;
            };
        }
#pragma warning restore CS1998


        private void foundRunningExeHandler()
        {
            string msg = "Running JDK processes have been found. Installation cannot continue. Please restart your computer, and rerun this installer without running anything else (including VS Code)";
            throw new InvalidOperationException(msg);
        }

        private string[]? GetExtractionIgnoreDirectories()
        {
            List<string> ignoreDirs = new List<string>();
            if (!installSelectionModel.InstallCpp) ignoreDirs.Add(configurationProvider.FullConfig.CppToolchain.Directory + "/");
            if (!installSelectionModel.InstallGradle) ignoreDirs.Add(configurationProvider.FullConfig.Gradle.ZipName);
            if (!installSelectionModel.InstallJDK) ignoreDirs.Add(configurationProvider.JdkConfig.Folder + "/");
            if (!installSelectionModel.InstallTools) ignoreDirs.Add(configurationProvider.UpgradeConfig.Tools.Folder + "/");
            if (!installSelectionModel.InstallWPILibDeps) ignoreDirs.Add(configurationProvider.UpgradeConfig.Maven.Folder + "/");

            return ignoreDirs.ToArray();
        }


        private async Task<(MemoryStream stream, Platform platform, byte[] hash)> DownloadToMemoryStream(Platform platform, string downloadUrl)
        {
            MemoryStream ms = new MemoryStream(100000000);
            // Download VS Code for current platform
            using var client = new HttpClientDownloadWithProgress(downloadUrl, ms);
            client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                if (progressPercentage != null)
                {
                    Progress = Convert.ToInt16(progressPercentage);
                    if (currentSpectreTask != null)
                        currentSpectreTask.Value = Progress;
                }
            };

            await client.StartDownload();

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
    }
}
