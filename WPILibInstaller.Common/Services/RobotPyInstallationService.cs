using System.Diagnostics;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.Services
{
    public sealed class RobotPyInstallationService : IRobotPyInstallationService
    {
        private readonly IConfigurationProvider configurationProvider;
        private readonly string pythonWindowsDir;
        private readonly string pythonPkgDir;
        private readonly string robotpyDir;
        private readonly string uvDir;
        public RobotPyInstallationService(IConfigurationProvider configurationProvider,
            IToInstallProvider toInstallProvider)
        {
            this.configurationProvider = configurationProvider;
            pythonWindowsDir = Path.Join(configurationProvider.InstallDirectory, configurationProvider.PythonConfig.Folder, configurationProvider.PythonConfig.ExeFile);
            pythonPkgDir = Path.Join(configurationProvider.InstallDirectory, configurationProvider.PythonConfig.Folder, configurationProvider.PythonConfig.PkgFile);
            uvDir = Path.Join(configurationProvider.InstallDirectory, configurationProvider.PythonConfig.Folder, configurationProvider.PythonConfig.UvInstallFile);
            robotpyDir = Path.Join(configurationProvider.InstallDirectory, configurationProvider.RobotpyConfig.Folder);
        }

        public async Task InstallPython(IProgress<InstallProgress>? progress = null)
        {
            progress?.Report(new InstallProgress(50, "Installing Python"));

            var currentPlatform = PlatformUtils.CurrentPlatform;
            var tempFile = Path.GetTempFileName();

            switch (currentPlatform)
            {
                case Platform.Win64:
                    await RunCommand(pythonWindowsDir, $"\"{tempFile}\" /quiet InstallAllUsers=0 Include_pip=1");
                    await RunCommand("cmd.exe", $"/c PowerShell.exe -ExecutionPolicy ByPass -File \"{uvDir}\"");
                    break;
                case Platform.MacArm64:
                case Platform.Mac64:
                    await RunCommand("/bin/bash", $"-c installer -pkg '{pythonPkgDir}' -target CurrentUserHomeDirectory");
                    await RunCommand("/bin/sh", $"-c chmod +x \"{uvDir}\"");
                    await RunCommand("/bin/sh", $"-c ./\"{uvDir}\"");
                    break;
                case Platform.Linux64:
                    await RunCommand("/bin/sh", $"-c chmod +x \"{uvDir}\"");
                    await RunCommand("/bin/sh", $"-c ./\"{uvDir}\"");
                    break;
                case Platform.LinuxArm64:
                    await RunCommand("/bin/sh", $"-c chmod +x \"{uvDir}\"");
                    await RunCommand("/bin/sh", $"-c ./\"{uvDir}\"");
                    break;
                default:
                    throw new PlatformNotSupportedException("Invalid platform");
            }
        }

        public async Task InstallRobotPy(IProgress<InstallProgress>? progress = null)
        {
            progress?.Report(new InstallProgress(50, "Installing robotpy"));
            string[] whlFiles = Directory.GetFiles(robotpyDir, "*.whl");
            var currentPlatform = PlatformUtils.CurrentPlatform;
            
            switch (currentPlatform)
            {
                case Platform.Win64:
                    await RunCommand("cmd.exe", $"/c for %w in (\"{robotpyDir}\\*.whl\") do py -3 -m pip install --user --no-index \"%w\"");
                    break;
                case Platform.MacArm64:
                case Platform.Mac64:
                    await RunCommand("python3", $"-c pip install --user --no-index --find-links={robotpyDir} {robotpyDir}/*.whl --break-system-packages");
                    break;
                case Platform.Linux64:
                    await RunCommand("/bin/sh", $"-c pip install --user --no-index --find-links={robotpyDir} {robotpyDir}/*.whl --break-system-packages");
                    break;
                case Platform.LinuxArm64:
                    await RunCommand("/bin/sh", $"-c pip install --user --no-index --find-links={robotpyDir} {robotpyDir}/*.whl --break-system-packages");
                    break;
                default:
                    throw new PlatformNotSupportedException("Invalid platform");
            }

        }

        public static async Task RunCommand(string fileName, string cmd)
        {
            var currentPlatform = PlatformUtils.CurrentPlatform;
            ProcessStartInfo startInfo = new ProcessStartInfo(fileName, cmd)
            {
                RedirectStandardOutput = false,
                UseShellExecute = true,
                CreateNoWindow = false
            };
            try
            {
                using var process = Process.Start(startInfo);
                process!.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Console.WriteLine("Installed successfully");
                }
                else
                {
                    Console.WriteLine("Installation failed");
                }
            }
            catch
            {
                Console.WriteLine("Error with Installation");
            }

        }
    }
}
