using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace WPILibInstaller.InstallTasks
{
    public static class Utilities
    {
        public static Task<bool> RunJavaJar(string installDir, string jar, int timeoutMs)
        {
            string java = Path.Join(installDir, "jdk", "bin", "java");
            if (OperatingSystem.IsWindows())
            {
                java += ".exe";
            }
            ProcessStartInfo pstart = new ProcessStartInfo(java, $"-jar \"{jar}\"");
            var p = Process.Start(pstart);
            return Task.Run(() =>
            {
                return p!.WaitForExit(timeoutMs);
            });
        }

        public static Task<bool> RunScriptExecutable(string script, int timeoutMs, params string[] args)
        {
            ProcessStartInfo pstart = new ProcessStartInfo(script, string.Join(" ", args));
            var p = Process.Start(pstart);
            return Task.Run(() =>
            {
                return p!.WaitForExit(timeoutMs);
            });
        }
    }
}
