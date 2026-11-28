using System.Diagnostics;

namespace WPILibInstaller.Utils
{
    public static class ProcessExecutionUtils
    {
        public static async Task<bool> RunExecutable(string path, int timeoutMs, CancellationToken token = default)
        {
            if (OperatingSystem.IsWindows())
            {
                path += ".exe";
            }

            using CancellationTokenSource timeoutSource = new(timeoutMs);
            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutSource.Token);

            ProcessStartInfo startInfo = new(path);
            using var process = Process.Start(startInfo);

            try
            {
                await process!.WaitForExitAsync(linkedSource.Token);
                return true;
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !token.IsCancellationRequested)
            {
                return false;
            }
        }

        public static async Task<bool> RunGradleStop(string gradleHome, string javaExecutable, string gradleUserHome, int timeoutMs, CancellationToken token = default)
        {
            string launcherJar = Directory
                .EnumerateFiles(Path.Combine(gradleHome, "lib"), "gradle-launcher-*.jar")
                .FirstOrDefault() ?? "";

            if (launcherJar.Length == 0)
            {
                return false;
            }

            ProcessStartInfo startInfo = new(javaExecutable)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = OperatingSystem.IsWindows(),
                WorkingDirectory = gradleHome,
            };
            startInfo.Environment["GRADLE_USER_HOME"] = gradleUserHome;
            startInfo.ArgumentList.Add("-classpath");
            startInfo.ArgumentList.Add(launcherJar);
            startInfo.ArgumentList.Add("org.gradle.launcher.GradleMain");
            startInfo.ArgumentList.Add("--stop");

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

            using CancellationTokenSource timeoutSource = new(timeoutMs);
            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutSource.Token);

            try
            {
                await process.WaitForExitAsync(linkedSource.Token);
                await Task.WhenAll(outputTask, errorTask);
                return true;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                }

                return false;
            }
        }

        public static async Task<bool> RunJavaJar(string installDir, string jar, int timeoutMs)
        {
            string java = Path.Join(installDir, "jdk", "bin", "java");
            if (OperatingSystem.IsWindows())
            {
                java += ".exe";
            }

            ProcessStartInfo startInfo = new(java, $"-jar \"{jar}\"");
            using var process = Process.Start(startInfo);

            if (timeoutMs == Timeout.Infinite)
            {
                await process!.WaitForExitAsync();
                return true;
            }

            using CancellationTokenSource timeoutSource = new(timeoutMs);
            try
            {
                await process!.WaitForExitAsync(timeoutSource.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public static async Task<bool> RunScriptExecutable(string script, int timeoutMs, CancellationToken token = default, params string[] args)
        {
            ProcessStartInfo startInfo = new(script, string.Join(" ", args));
            using var process = Process.Start(startInfo);

            if (timeoutMs == Timeout.Infinite)
            {
                await process!.WaitForExitAsync(token);
                return true;
            }

            using CancellationTokenSource timeoutSource = new(timeoutMs);
            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutSource.Token);

            try
            {
                await process!.WaitForExitAsync(linkedSource.Token);
                return true;
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !token.IsCancellationRequested)
            {
                return false;
            }
        }

        // Runs an executable with administrator rights and waits for it, asking the user to allow it
        // unless this process already has them: UAC on Windows, the administrator password prompt on
        // macOS, and pkexec on Linux. Returns whether it ran and succeeded.
        public static async Task<bool> RunElevated(string path, params string[] args)
        {
            ProcessStartInfo startInfo;
            if (Environment.IsPrivilegedProcess)
            {
                startInfo = new(path, args);
            }
            else if (OperatingSystem.IsWindows())
            {
                startInfo = new(path, string.Join(' ', args.Select(QuoteWindows))) { UseShellExecute = true, Verb = "runas" };
            }
            else if (OperatingSystem.IsMacOS())
            {
                var command = string.Join(' ', args.Prepend(path).Select(QuoteShell));
                startInfo = new("/usr/bin/osascript", ["-e", $"do shell script {QuoteAppleScript(command)} with administrator privileges"]);
            }
            else
            {
                startInfo = new("pkexec", args.Prepend(path));
            }

            try
            {
                using var process = Process.Start(startInfo);
                await process!.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // The user declined, or there's no way to ask (no pkexec)
                return false;
            }
        }

        private static string QuoteWindows(string arg) => arg.Contains(' ', StringComparison.Ordinal) ? $"\"{arg}\"" : arg;

        private static string QuoteShell(string arg) => "'" + arg.Replace("'", "'\\''", StringComparison.Ordinal) + "'";

        private static string QuoteAppleScript(string text) =>
            "\"" + text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}
