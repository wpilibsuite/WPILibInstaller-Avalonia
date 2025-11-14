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
        public string TextTotal { get; set; } = "";

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
                TextTotal = task.TextTotal;
            }
        }


        public async Task Install()
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Starting installation...", async ctx =>
                {
                    {
                        ctx.Status("Extracting archive...");
                        var task = new ExtractArchiveTask(
                            configurationProvider, null 
                        );

                        task.Attach(this); // Subscribe to progress changes
                        try
                        {
                            await task.Execute(token);
                        }

                        // Handle if a running exe was found
                        catch (FoundRunningExeException)
                        {
                            foundRunningExeHandler();
                        }
                        task.Detach(this); // Unsubscribe from progress changes
                    }

                    if (installSelectionModel.InstallGradle)
                    {
                        ctx.Status("Installing Gradle...");
                        var task = new GradleSetupTask(
                            configurationProvider
                        );
                        task.Attach(this);
                        await task.Execute(token);
                        task.Detach(this);
                    }

                    if (installSelectionModel.InstallTools)
                    {
                        ctx.Status("Installing Tools...");
                        TextTotal = "Installing Tools";
                        var task = new ToolSetupTask(
                            configurationProvider
                        );
                        task.Attach(this);
                        await task.Execute(token);
                    }

                    if (installSelectionModel.InstallCpp)
                    {
                        ctx.Status("Installing C++...");
                        var task = new CppSetupTask(
                            configurationProvider
                        );
                        task.Attach(this);
                        await task.Execute(token);
                    }

                    {
                        ctx.Status("Fixing Maven metadata...");
                        var task = new MavenMetaDataFixerTask(
                            configurationProvider
                        );
                        task.Attach(this);
                        await task.Execute(token);
                    }

                    if (installSelectionModel.InstallVsCode)
                    {
                        ctx.Status("Downloading VS Code...");
                        await DownloadVsCode();

                        {
                            ctx.Status("Extracting VS Code...");
                            var task = new VsCodeSetupTask(
                                configurationProvider.VsCodeModel, configurationProvider
                            );
                            task.Attach(this);
                            await task.Execute(token);
                        }

                        {
                            ctx.Status("Configuring VS Code...");
                            var task = new ConfigureVsCodeSettingsTask(
                                configurationProvider.VsCodeModel, configurationProvider
                            );
                            task.Attach(this);
                        }

                        {
                            ctx.Status("Installing VS Code Extensions...");
                            var task = new VsCodeExtensionsSetupTask(
                                configurationProvider.VsCodeModel, configurationProvider
                            );
                            task.Attach(this);
                            await task.Execute(token);
                        }
                    }

                    ctx.Status("Creating Shortcuts...");
                    {
                        var task = new ShortcutCreatorTask(
                            configurationProvider.VsCodeModel, 
                            configurationProvider, 
                            installSelectionModel.InstallAsAdmin, 
                            installSelectionModel.InstallDocs 
                        );
                        task.Attach(this);

                        // Define what to do if UAC times out (windows)
                        task.uacTimeoutCallback = uacTimeoutCallback;

                        try
                        {
                            await task.Execute(token);
                        }

                        // Handle shortcut creator failing
                        catch (ShortcutCreationFailedException err)
                        {
                            AnsiConsole.WriteLine(
                                $"[red]Shortcut creation failed with error code {err.Message}[/]"
                            );
                            throw;
                        }
                    }

                    ctx.Status("Installation complete!");
                    await Task.Delay(500); // tiny pause to show final message
                });
        }

        private static Func<Task<bool>> getUacTimeoutCallback()
        {
            return async () =>
            {
                var prompt = new Spectre.Console.ConfirmationPrompt(
                    "UAC Prompt Cancelled or Timed Out. Would you like to retry?"
                );

                return await Spectre.Console.AnsiConsole.PromptAsync(prompt);
            };
        }


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
