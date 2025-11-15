using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WPILibInstaller.Interfaces;

namespace WPILibInstaller.InstallTasks
{
    public class FoundRunningExeException : Exception
    {
        public FoundRunningExeException()
            : base()
        {
        }
    }

    public class ExtractArchiveTask : InstallTask
    {

        private string[]? filter;

        public ExtractArchiveTask(
            IConfigurationProvider pConfigurationProvider, string[]? pFilter
        )
        : base(pConfigurationProvider)
        {
            filter = pFilter;
        }

        override public async Task Execute(CancellationToken token)
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
                    throw new FoundRunningExeException();
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

                if (extractor.EntryIsExecutable && !OperatingSystem.IsWindows())
                {
                    var currentMode = File.GetUnixFileMode(fullZipToPath);
                    File.SetUnixFileMode(fullZipToPath, currentMode | UnixFileMode.GroupExecute | UnixFileMode.UserExecute | UnixFileMode.OtherExecute);
                }
            }

        }
    }
}
