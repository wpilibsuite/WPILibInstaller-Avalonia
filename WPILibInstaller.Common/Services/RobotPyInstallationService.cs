using System.Diagnostics;
using System.Text.RegularExpressions;
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
        private readonly string robotpyWhlFile;
        private readonly Boolean isAdmin;
        public RobotPyInstallationService(IConfigurationProvider configurationProvider,
            IToInstallProvider toInstallProvider)
        {
            this.configurationProvider = configurationProvider;
            pythonWindowsDir = Path.Join(configurationProvider.InstallDirectory, configurationProvider.PythonConfig.Folder, configurationProvider.PythonConfig.ExeFile);
            pythonPkgDir = Path.Join(configurationProvider.InstallDirectory, configurationProvider.PythonConfig.Folder, configurationProvider.PythonConfig.PkgFile);
            robotpyDir = Path.Join(configurationProvider.InstallDirectory, configurationProvider.RobotpyConfig.Folder);
            robotpyWhlFile = Path.Join(robotpyDir, configurationProvider.RobotpyConfig.WhlFile);
            isAdmin = toInstallProvider.Model.InstallAsAdmin;
        }

        public async Task InstallPython(IProgress<InstallProgress>? progress = null)
        {
            progress?.Report(new InstallProgress(50, "Installing Python"));

            var currentPlatform = PlatformUtils.CurrentPlatform;

            switch (currentPlatform)
            {
                case Platform.Win64:
                    var tempFile = Path.GetTempFileName();
                    var startInfo = new ProcessStartInfo(pythonWindowsDir, $"\"{tempFile}\" /passive InstallAllUsers=0 Include_pip=1")
                    {
                        WorkingDirectory = Environment.CurrentDirectory,
                    };
                    await RunCommand(startInfo);
                    break;
                case Platform.MacArm64:
                case Platform.Mac64:
                    startInfo = new ProcessStartInfo("/bin/bash", "-c sudo installer -pkg ./" + configurationProvider.PythonConfig.PkgFile + " -target \\")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = false
                    };
                    await RunCommand(startInfo);
                    break;
                case Platform.Linux64:
                    startInfo = new ProcessStartInfo("/bin/sh", "-c sudo apt-get update -y")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = false
                    };
                    await RunCommand(startInfo);
                    startInfo = new ProcessStartInfo("/bin/sh", "-c sudo apt-get install -y python3 python3-pip python3-venv")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = false
                    };
                    await RunCommand(startInfo);
                    break;
                case Platform.LinuxArm64:
                    startInfo = new ProcessStartInfo("/bin/sh", "-c sudo apt-get update -y")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = false
                    };
                    await RunCommand(startInfo);
                    startInfo = new ProcessStartInfo("/bin/sh", "-c sudo apt-get install -y python3 python3-pip python3-venv python3-wheel pipx")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = false
                    };
                    await RunCommand(startInfo);
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
                    startInfo = new ProcessStartInfo("cmd.exe")
                    {
                        Arguments = $"/c python -m pip install \"{robotpyWhlFile}\"",
                        RedirectStandardOutput = false,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                    await RunCommand(startInfo);
                    break;
                case Platform.MacArm64:
                case Platform.Mac64:
                    startInfo = new ProcessStartInfo("python3")
                    {
                        Arguments = $"-m pip3 install \"{robotpyWhlFile}\"",
                        RedirectStandardOutput = false,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                    await RunCommand(startInfo);
                    break;
                case Platform.Linux64:
                    startInfo = new ProcessStartInfo("/bin/sh")
                    {
                        Arguments = $"-c pipx install \"{robotpyWhlFile}\"",
                        RedirectStandardOutput = false,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                    await RunCommand(startInfo);
                    break;
                case Platform.LinuxArm64:
                    startInfo = new ProcessStartInfo("/bin/sh")
                    {
                        Arguments = $"-c pipx install \"{robotpyWhlFile}\"",
                        RedirectStandardOutput = false,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                    await RunCommand(startInfo);
                    break;
                default:
                    throw new PlatformNotSupportedException("Invalid platform");
            }

        }

        public static async Task RunCommand(ProcessStartInfo startInfo)
        {
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
