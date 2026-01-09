using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.Services
{
    public class ToolInstallationService : IToolInstallationService
    {
        private readonly IConfigurationProvider configurationProvider;

        public ToolInstallationService(IConfigurationProvider configurationProvider)
        {
            this.configurationProvider = configurationProvider;
        }

        public Task RunGradleSetup(IProgress<InstallProgress>? progress = null)
        {
            progress?.Report(new InstallProgress(50, "Configuring Gradle"));

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

        public async Task RunToolSetup(IProgress<InstallProgress>? progress = null)
        {
            progress?.Report(new InstallProgress(50, "Configuring Tools"));

            await ProcessExecutionUtils.RunJavaJar(configurationProvider.InstallDirectory,
                Path.Combine(configurationProvider.InstallDirectory,
                configurationProvider.UpgradeConfig.Tools.Folder,
                configurationProvider.UpgradeConfig.Tools.UpdaterJar), 30000);
        }

        public async Task RunCppSetup(IProgress<InstallProgress>? progress = null)
        {
            progress?.Report(new InstallProgress(50, "Configuring C++"));

            await Task.Yield();
        }

        public async Task RunMavenMetaDataFixer(IProgress<InstallProgress>? progress = null)
        {
            progress?.Report(new InstallProgress(50, "Fixing up maven metadata"));

            await ProcessExecutionUtils.RunJavaJar(configurationProvider.InstallDirectory,
                Path.Combine(configurationProvider.InstallDirectory,
                configurationProvider.UpgradeConfig.Maven.Folder,
                configurationProvider.UpgradeConfig.Maven.MetaDataFixerJar), 20000);
        }
    }
}
