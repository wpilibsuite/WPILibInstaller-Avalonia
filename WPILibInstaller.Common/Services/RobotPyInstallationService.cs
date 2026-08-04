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
        private readonly string pythonVersion;
        private readonly string robotpyWhlFile;
        private readonly Boolean isAdmin;
        public RobotPyInstallationService(IConfigurationProvider configurationProvider,
            IToInstallProvider toInstallProvider)
        {
            this.configurationProvider = configurationProvider;
            pythonWindowsDir = Path.Join(configurationProvider.InstallDirectory, configurationProvider.PythonConfig.Folder, configurationProvider.PythonConfig.ExeFile);
            pythonPkgDir = Path.Join(configurationProvider.InstallDirectory, configurationProvider.PythonConfig.Folder, configurationProvider.PythonConfig.PkgFile);
            pythonVersion = configurationProvider.PythonConfig.Version;
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
                    await RunCommand("cmd.exe", "/c powershell -ExecutionPolicy ByPass -c \"irm https://astral.sh/uv/install.ps1 | iex\"");
                    break;
                case Platform.MacArm64:
                case Platform.Mac64:
                    await RunCommand("/bin/bash", $"-c installer -pkg '{pythonPkgDir}' -target CurrentUserHomeDirectory");
                    await RunCommand("/bin/sh", "-c curl -LsSf https://astral.sh/uv/install.sh | sh");
                    break;
                case Platform.Linux64:
                    await RunCommand("/bin/sh", "-c python3 -m pip install --user pipx");
                    await RunCommand("/bin/sh", "-c python3 -m pipx ensurepath");
                    await RunCommand("/bin/sh", "-c curl -LsSf https://astral.sh/uv/install.sh | sh");
                    break;
                case Platform.LinuxArm64:
                    await RunCommand("/bin/sh", "-c python3 -m pip install --user pipx");
                    await RunCommand("/bin/sh", "-c python3 -m pipx ensurepath");
                    await RunCommand("/bin/sh", "-c curl -LsSf https://astral.sh/uv/install.sh | sh");
                    break;
                default:
                    throw new PlatformNotSupportedException("Invalid platform");
            }
        }

        public async Task InstallRobotPy(IProgress<InstallProgress>? progress = null)
        {
            progress?.Report(new InstallProgress(50, "Installing robotpy"));
            var currentPlatform = PlatformUtils.CurrentPlatform;
            
            switch (currentPlatform)
            {
                case Platform.Win64:
                    await RunCommand("cmd.exe", $"/c python -m pip install \"{robotpyWhlFile}\"");
                    break;
                case Platform.MacArm64:
                case Platform.Mac64:
                    await RunCommand("python3", $"-m pip install \"{robotpyWhlFile}\"");
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
