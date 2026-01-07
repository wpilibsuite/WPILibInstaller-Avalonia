using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace WPILibInstaller.Utils
{
    public static class ProcessExecutionUtils
    {
        public static Task<bool> RunJavaJar(string installDir, string jar, int timeoutMs, CancellationToken cancellationToken = default)
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
                if (cancellationToken.IsCancellationRequested)
                {
                    p?.Kill();
                    return false;
                }
                return p!.WaitForExit(timeoutMs);
            }, cancellationToken);
        }

        public static Task<bool> RunScriptExecutable(string script, int timeoutMs, CancellationToken cancellationToken = default, params string[] args)
        {
            ProcessStartInfo pstart = new ProcessStartInfo(script, string.Join(" ", args));
            var p = Process.Start(pstart);
            return Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    p?.Kill();
                    return false;
                }
                return p!.WaitForExit(timeoutMs);
            }, cancellationToken);
        }
    }
}
