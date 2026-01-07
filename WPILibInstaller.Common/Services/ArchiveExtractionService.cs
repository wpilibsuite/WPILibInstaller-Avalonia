using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;


namespace WPILibInstaller.Services
{
    public class ArchiveExtractionService : IArchiveExtractionService
    {
        private readonly IConfigurationProvider configurationProvider;
        private readonly IProgramWindow programWindow;

        public ArchiveExtractionService(IConfigurationProvider configurationProvider, IProgramWindow programWindow)
        {
            this.configurationProvider = configurationProvider;
            this.programWindow = programWindow;
        }

        public async Task ExtractJDKAndTools(CancellationToken token, IProgress<InstallProgress>? progress = null)
        {
            await ExtractArchive(token, new[] {
                configurationProvider.JdkConfig.Folder + "/",
                configurationProvider.UpgradeConfig.Tools.Folder + "/",
                configurationProvider.AdvantageScopeConfig.Folder + "/",
                configurationProvider.ElasticConfig.Folder + "/",
                "installUtils/", "icons"}, progress);
        }

        public async Task ExtractArchive(CancellationToken token, string[]? filter, IProgress<InstallProgress>? progress = null)
        {
            progress?.Report(new InstallProgress(0, "Starting extraction"));

            if (OperatingSystem.IsWindows())
            {
                progress?.Report(new InstallProgress(0, "Checking for currently running JDKs"));
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
                    await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        ContentTitle = "JDKs Running",
                        ContentMessage = msg,
                        Icon = MsBox.Avalonia.Enums.Icon.Error,
                        ButtonDefinitions = MsBox.Avalonia.Enums.ButtonEnum.Ok
                    }).ShowWindowDialogAsync(programWindow.Window);
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
    }
}
