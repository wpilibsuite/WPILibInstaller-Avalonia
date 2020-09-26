using System;
using System.Diagnostics;

namespace WPILibInstaller_Avalonia.Utils
{
    public static class ProcessUtils
    {
        public static string GetStartingExecutablePath()
        {
            return System.AppContext.BaseDirectory;
        }
    }
}
