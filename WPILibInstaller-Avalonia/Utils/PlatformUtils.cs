using System.Runtime.InteropServices;

namespace WPILibInstaller.Utils
{
    public enum Platform
    {
        Win32,
        Win64,
        Linux64,
        Mac64,
        Invalid
    }

    public class PlatformUtils
    {
        static PlatformUtils()
        {
            CurrentPlatform = Platform.Invalid;

            var currentArch = RuntimeInformation.OSArchitecture;
            if (currentArch != Architecture.X64 && currentArch != Architecture.X86)
            {
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (currentArch == Architecture.X64)
                {
                    CurrentPlatform = Platform.Linux64;
                }
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (currentArch == Architecture.X64)
                {
                    CurrentPlatform = Platform.Mac64;
                }
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (currentArch == Architecture.X64)
                {
                    CurrentPlatform = Platform.Win64;
                }
                else
                {
                    CurrentPlatform = Platform.Win32;
                }
                return;
            }
        }

        public static Platform CurrentPlatform { get; }
    }
}
