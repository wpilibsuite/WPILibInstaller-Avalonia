using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.Services
{
    public sealed class ToolInstallationService : IToolInstallationService
    {
        private readonly IConfigurationProvider configurationProvider;

        public ToolInstallationService(IConfigurationProvider configurationProvider)
        {
            this.configurationProvider = configurationProvider;
        }

        public async Task StopGradleDaemons(IProgress<InstallProgress>? progress = null, CancellationToken token = default)
        {
            progress?.Report(new InstallProgress(0, "Stopping Gradle daemons"));

            // Only use the JDK installed at the current install location. On a fresh
            // install there is no JDK yet, so there can be no stale daemons to stop.
            string javaExecutable = Path.Join(
                configurationProvider.InstallDirectory,
                "jdk",
                "bin",
                OperatingSystem.IsWindows() ? "java.exe" : "java");
            if (!File.Exists(javaExecutable))
            {
                return;
            }

            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(userFolder))
            {
                return;
            }

            string gradleHome = Path.Combine(userFolder, ".gradle");
            string permGradleHome = Path.Combine(gradleHome, "permwrapper");
            string distsFolder = Path.Combine(permGradleHome, "dists");

            // Only permwrapper distributions are installed by WPILib. The regular wrapper
            // cache is left alone because those Gradle versions belong to other projects.
            var installedGradleHomes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (Directory.Exists(distsFolder))
                {
                    foreach (string launcherJar in Directory.EnumerateFiles(distsFolder, "gradle-launcher-*.jar", SearchOption.AllDirectories))
                    {
                        string? libFolder = Path.GetDirectoryName(launcherJar);
                        string? gradleDistributionHome = libFolder == null ? null : Path.GetDirectoryName(libFolder);
                        if (gradleDistributionHome != null)
                        {
                            string version = Path.GetFileName(gradleDistributionHome);
                            installedGradleHomes.TryAdd(version, gradleDistributionHome);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Best effort only. Any daemon that cannot be found is still caught by the
                // running JDK check performed during extraction.
            }

            // WPILib distributions live in permwrapper, but their daemon registries are in
            // the standard Gradle user home (~/.gradle/daemon/<version>). Also check a
            // permwrapper-scoped user home in case a project configures one.
            var gradleUserHomes = new List<string> { gradleHome };
            if (Directory.Exists(permGradleHome))
            {
                gradleUserHomes.Add(permGradleHome);
            }

            foreach (string distributionHome in installedGradleHomes.Values)
            {
                foreach (string userHome in gradleUserHomes)
                {
                    try
                    {
                        await ProcessExecutionUtils.RunGradleStop(distributionHome, javaExecutable, userHome, 30000, token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        // Best effort only. Any daemon that cannot be stopped is still caught
                        // by the running JDK check performed during extraction.
                    }
                }
            }
        }

        public Task RunGradleSetup(IProgress<InstallProgress>? progress = null)
        {
            progress?.Report(new InstallProgress(50, "Configuring Gradle"));

            string extractFolder = configurationProvider.InstallDirectory;
            var config = configurationProvider.FullConfig;
            string gradleZipLoc = Path.Combine(extractFolder, "installUtils", config.Gradle.ZipName);

            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            List<Task> tasks = new();
            foreach (var extractLocation in config.Gradle.ExtractLocations)
            {
                string toFolder = Path.Combine(userFolder, ".gradle", extractLocation, Path.GetFileNameWithoutExtension(config.Gradle.ZipName), config.Gradle.Hash);
                string toFile = Path.Combine(toFolder, config.Gradle.ZipName);
                tasks.Add(Task.Run(() =>
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

            await ProcessExecutionUtils.RunExecutable(
                Path.Combine(
                    configurationProvider.InstallDirectory,
                    configurationProvider.UpgradeConfig.Tools.Folder,
                    configurationProvider.UpgradeConfig.Tools.UpdaterExe),
                30000);
        }

        public async Task RunCppSetup(IProgress<InstallProgress>? progress = null)
        {
            progress?.Report(new InstallProgress(50, "Configuring C++"));
            await Task.Yield();
        }

        public async Task RunMavenMetaDataFixer(IProgress<InstallProgress>? progress = null)
        {
            progress?.Report(new InstallProgress(50, "Fixing up maven metadata"));

            await ProcessExecutionUtils.RunJavaJar(
                configurationProvider.InstallDirectory,
                configurationProvider.JdkConfig.Folder,
                Path.Combine(
                    configurationProvider.InstallDirectory,
                    configurationProvider.UpgradeConfig.Maven.Folder,
                    configurationProvider.UpgradeConfig.Maven.MetaDataFixerJar),
                20000);
        }
    }
}
