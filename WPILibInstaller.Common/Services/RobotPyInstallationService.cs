using System.Formats.Tar;
using System.IO.Compression;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.Services
{
    public sealed class RobotPyInstallationService : IRobotPyInstallationService
    {
        private readonly IConfigurationProvider configurationProvider;

        public RobotPyInstallationService(IConfigurationProvider configurationProvider)
        {
            this.configurationProvider = configurationProvider;
        }

        public static string GetPythonDirectory(IConfigurationProvider configurationProvider)
        {
            return Path.Join(configurationProvider.InstallDirectory, configurationProvider.PythonConfig.Folder);
        }

        public static string GetPythonExecutable(IConfigurationProvider configurationProvider)
        {
            var pythonDirectory = GetPythonDirectory(configurationProvider);
            return OperatingSystem.IsWindows()
                ? Path.Join(pythonDirectory, "python.exe")
                : Path.Join(pythonDirectory, "bin", "python3");
        }

        public static IEnumerable<string> GetPathDirectories(IConfigurationProvider configurationProvider)
        {
            var pythonDirectory = GetPythonDirectory(configurationProvider);
            if (OperatingSystem.IsWindows())
            {
                yield return pythonDirectory;
                yield return Path.Join(pythonDirectory, "Scripts");
            }
            else
            {
                yield return Path.Join(pythonDirectory, "bin");
            }

            yield return Path.Join(configurationProvider.InstallDirectory, configurationProvider.PythonConfig.UvFolder);
        }

        public async Task InstallPython(CancellationToken token, IProgress<InstallProgress>? progress = null)
        {
            var config = configurationProvider.PythonConfig;
            var installDirectory = configurationProvider.InstallDirectory;
            var archiveDirectory = Path.Join(installDirectory, config.ArchiveFolder);
            var pythonDirectory = GetPythonDirectory(configurationProvider);

            progress?.Report(new InstallProgress(0, $"Extracting Python {config.Version}"));

            if (Directory.Exists(pythonDirectory))
            {
                Directory.Delete(pythonDirectory, recursive: true);
            }

            await Task.Run(async () =>
            {
                await using var fileStream = File.OpenRead(Path.Join(archiveDirectory, config.ArchiveFile));
                await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                await TarFile.ExtractToDirectoryAsync(gzipStream, installDirectory, overwriteFiles: true, token);
            }, token);

            TryDeleteDirectory(archiveDirectory);

            progress?.Report(new InstallProgress(100, $"Installed Python {config.Version}"));
        }

        public async Task InstallRobotPy(CancellationToken token, IProgress<InstallProgress>? progress = null)
        {
            var config = configurationProvider.RobotpyConfig;
            var wheelDirectory = Path.Join(configurationProvider.InstallDirectory, config.Folder);

            progress?.Report(new InstallProgress(0, $"Installing RobotPy {config.Version}"));

            var result = await ProcessExecutionUtils.RunProcessAsync(
                GetPythonExecutable(configurationProvider),
                [
                    "-I", "-m", "pip", "--isolated", "install",
                    "--no-index",
                    "--find-links", wheelDirectory,
                    "--disable-pip-version-check",
                    "--no-input",
                    "--no-warn-script-location",
                    $"robotpy=={config.Version}",
                ],
                line => progress?.Report(new InstallProgress(0, line)),
                token);

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"RobotPy installation failed with exit code {result.ExitCode}:{Environment.NewLine}{result.Output}");
            }

            TryDeleteDirectory(wheelDirectory);

            progress?.Report(new InstallProgress(100, $"Installed RobotPy {config.Version}"));
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
