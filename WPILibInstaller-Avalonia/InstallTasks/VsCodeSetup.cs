using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WPILibInstaller.Interfaces;

namespace WPILibInstaller.InstallTasks
{
    public class VsCodeSetupTask : InstallTask
    {

        private readonly IVsCodeInstallLocationProvider vsInstallProvider;

        public VsCodeSetupTask(
            IVsCodeInstallLocationProvider pVsInstallProvider,
            IConfigurationProvider pConfigurationProvider
        )
        :base(pConfigurationProvider)
        {
            vsInstallProvider = pVsInstallProvider;
        }

        public override async Task Execute(CancellationToken token)
        {
            if (!vsInstallProvider.Model.InstallingVsCode) return;

            Text = "Installing VS Code";
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
                await Utilities.RunScriptExecutable("unzip", Timeout.Infinite, zipPath, "-d", intoPath);
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

                if (extractor.EntryIsExecutable && !OperatingSystem.IsWindows())
                {
                    var currentMode = File.GetUnixFileMode(fullZipToPath);
                    File.SetUnixFileMode(fullZipToPath, currentMode | UnixFileMode.GroupExecute | UnixFileMode.UserExecute | UnixFileMode.OtherExecute);
                }
            }

        }
    }
}
