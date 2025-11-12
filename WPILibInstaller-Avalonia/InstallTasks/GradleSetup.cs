using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPILibInstaller.Interfaces;

namespace WPILibInstaller.InstallTasks
{
    public class GradleSetupTask : InstallTask
    {

        public GradleSetupTask(
            IConfigurationProvider pConfigurationProvider
        )
        :base(pConfigurationProvider)
        {
        }

        public override async Task Execute(CancellationToken token)
        {
            Text = "Configuring Gradle";
            Progress = 50;

            string extractFolder = configurationProvider.InstallDirectory;
            var config = configurationProvider.FullConfig;

            string gradleZipLoc = Path.Combine(extractFolder, "installUtils", config.Gradle.ZipName);
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var tasks = config.Gradle.ExtractLocations.Select(async extractLocation =>
            {
                string toFolder = Path.Combine(
                    userFolder,
                    ".gradle",
                    extractLocation,
                    Path.GetFileNameWithoutExtension(config.Gradle.ZipName),
                    config.Gradle.Hash
                );

                string toFile = Path.Combine(toFolder, config.Gradle.ZipName);

                try
                {
                    Directory.CreateDirectory(toFolder);
                }
                catch (IOException)
                {
                    // safe to ignore
                }

                // Asynchronously copy instead of File.Copy
                await CopyFileAsync(gradleZipLoc, toFile, overwrite: true, token);
            });

            await Task.WhenAll(tasks);
        }

        private static async Task CopyFileAsync(string sourcePath, string destPath, bool overwrite, CancellationToken token)
        {
            if (!overwrite && File.Exists(destPath))
                return;

            // Ensure destination is writable
            await using FileStream sourceStream = File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using FileStream destinationStream = File.Open(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await sourceStream.CopyToAsync(destinationStream, 81920, token);
        }
    }
}
