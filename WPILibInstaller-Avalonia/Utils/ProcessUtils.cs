using System;
using System.Diagnostics;

namespace WPILibInstaller_Avalonia.Utils
{
    public static class ProcessUtils
    {
        public static string GetStartingExecutablePath()
        {
            string executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!;
            string executable = System.IO.Path.GetFileNameWithoutExtension(executablePath);

            if ("dotnet".Equals(executable, StringComparison.InvariantCultureIgnoreCase))
            {
                return typeof(ProcessUtils).Assembly.Location;
            }
            else
            {
                return executablePath;
            }
        }
    }
}
