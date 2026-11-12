using System.Runtime.InteropServices;

namespace WPILibInstaller.Utils
{
    public enum Platform
    {
        Win64,
        Linux64,
        LinuxArm64,
        Mac64,
        MacArm64,
        WinArm64,
        Invalid
    }

    public class PlatformUtils
    {
        public static string? GetArchitectureMismatchMessage()
        {
            var processArchitecture = RuntimeInformation.ProcessArchitecture;
            var osArchitecture = RuntimeInformation.OSArchitecture;
            if (processArchitecture == osArchitecture)
            {
                return null;
            }

            var operatingSystem = OperatingSystem.IsWindows() ? "Windows"
                : OperatingSystem.IsMacOS() ? "macOS"
                : OperatingSystem.IsLinux() ? "Linux"
                : "this operating system";
            var processArchitectureName = GetArchitectureName(processArchitecture);
            var osArchitectureName = GetArchitectureName(osArchitecture);

            return $"This is the {processArchitectureName} WPILib installer, but it is running on {operatingSystem} {osArchitectureName}. "
                + $"Download and run the {operatingSystem} {osArchitectureName} installer instead.";
        }

        private static string GetArchitectureName(Architecture architecture)
        {
            return architecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "ARM64",
                Architecture.X86 => "x86",
                Architecture.Arm => "ARM",
                _ => architecture.ToString()
            };
        }

        static PlatformUtils()
        {
            CurrentPlatform = Platform.Invalid;

            var currentArch = RuntimeInformation.OSArchitecture;
            if (currentArch != Architecture.X64 && currentArch != Architecture.Arm64)
            {
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (currentArch == Architecture.X64)
                {
                    CurrentPlatform = Platform.Linux64;
                }
                else if (currentArch == Architecture.Arm64)
                {
                    CurrentPlatform = Platform.LinuxArm64;
                }
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (currentArch == Architecture.X64)
                {
                    CurrentPlatform = Platform.Mac64;
                }
                else
                {
                    CurrentPlatform = Platform.MacArm64;
                }
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (currentArch == Architecture.X64)
                {
                    CurrentPlatform = Platform.Win64;
                }
                else if (currentArch == Architecture.Arm64)
                {
                    CurrentPlatform = Platform.WinArm64;
                }
                return;
            }
        }

        public static Platform CurrentPlatform { get; }
    }
}
