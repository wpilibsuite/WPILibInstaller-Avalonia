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

        public static async Task<bool> RunJavaJar(string installDir, string jdkFolder, string jar, int timeoutMs)
        {
            string java = Path.Join(installDir, jdkFolder, "bin", "java");
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
    }
}
