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
        private readonly string robotpyWhlFile;
        private readonly Boolean isAdmin;
        public RobotPyInstallationService(IConfigurationProvider configurationProvider,
            IToInstallProvider toInstallProvider)
        {
            this.configurationProvider = configurationProvider;
            pythonWindowsDir = Path.Join(configurationProvider.InstallDirectory, configurationProvider.PythonConfig.Folder, configurationProvider.PythonConfig.ExeFile);
            pythonPkgDir = Path.Join(configurationProvider.InstallDirectory, configurationProvider.PythonConfig.Folder, configurationProvider.PythonConfig.PkgFile);
            robotpyWhlFile = Path.Join(configurationProvider.InstallDirectory, configurationProvider.RobotpyConfig.Folder, configurationProvider.RobotpyConfig.WhlFile);
            isAdmin = toInstallProvider.Model.InstallAsAdmin;
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
                    break;
                case Platform.MacArm64:
                case Platform.Mac64:
                    await RunCommand("/bin/bash", "-c sudo installer -pkg ./" + pythonPkgDir + " -target \\");
                    break;
                case Platform.Linux64:
                    await RunCommand("/bin/sh", "-c sudo apt-get update -y");
                    await RunCommand("/bin/sh", "-c sudo apt-get install -y python3 python3-pip python3-venv");
                    break;
                case Platform.LinuxArm64:
                    await RunCommand("/bin/sh", "-c sudo apt-get update -y");
                    await RunCommand("/bin/sh", "-c sudo apt-get install -y python3 python3-pip python3-venv");
                    break;
                default:
                    throw new PlatformNotSupportedException("Invalid platform");
            }
        }

        public async Task InstallRobotPy(IProgress<InstallProgress>? progress = null)
        {
            progress?.Report(new InstallProgress(50, "Installing robotpy"));
            var currentPlatform = PlatformUtils.CurrentPlatform;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            switch (currentPlatform)
            {
                case Platform.Win64:
                    await RunCommand("cmd.exe", $"/c python -m pip install \"{robotpyWhlFile}\"");
                    break;
                case Platform.MacArm64:
                case Platform.Mac64:
                    await RunCommand("python3", $"-m pip3 install \"{robotpyWhlFile}\"");
                    break;
                case Platform.Linux64:
                    await RunCommand("/bin/sh", $"-c pipx install \"{robotpyWhlFile}\"");
                    break;
                case Platform.LinuxArm64:
                    await RunCommand("/bin/sh", $"-c pipx install \"{robotpyWhlFile}\"");
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
