using System.Runtime.InteropServices;

namespace WPILibInstaller.Utils
{
    public enum Platform
    {
        Win64,
        Linux64,
        LinuxArm64,
        LinuxArm32,
        Mac64,
        MacArm64,
        Invalid
    }

    public class PlatformUtils
    {
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
                else if (currentArch == Architecture.Arm)
                {
                    CurrentPlatform = Platform.LinuxArm32;
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
                return;
            }
        }

        public static Platform CurrentPlatform { get; }
    }
}
