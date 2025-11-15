using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace WPILibInstaller.InstallTasks
{
    public static class Utilities
    {
        public static async Task<bool> RunJavaJar(string installDir, string jar, int timeoutMs)
        {
            string java = Path.Join(installDir, "jdk", "bin", "java");
            if (OperatingSystem.IsWindows())
                java += ".exe";

            ProcessStartInfo pstart = new ProcessStartInfo(java, $"-jar \"{jar}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(pstart)!;

            // Begin async draining immediately
            var drainStdOut = process.StandardOutput.ReadToEndAsync();
            var drainStdErr = process.StandardError.ReadToEndAsync();

            // Wait with timeout
            var exited = await WaitForExitAsync(process, timeoutMs);

            // Ensure drains complete (even if timeout happened)
            await Task.WhenAll(drainStdOut, drainStdErr);

            return exited;
        }

        public static async Task<bool> RunScriptExecutable(string script, int timeoutMs, params string[] args)
        {
            ProcessStartInfo pstart = new ProcessStartInfo(script, string.Join(" ", args))
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(pstart)!;

            var drainStdOut = process.StandardOutput.ReadToEndAsync();
            var drainStdErr = process.StandardError.ReadToEndAsync();

            var exited = await WaitForExitAsync(process, timeoutMs);

            await Task.WhenAll(drainStdOut, drainStdErr);

            return exited;
        }

        private static async Task<bool> WaitForExitAsync(Process process, int timeoutMs)
        {
            try
            {
                // returns true if process exits in time
                return await Task.Run(() => process.WaitForExit(timeoutMs));
            }
            catch
            {
                return false;
            }
        }
    }
}
