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

        public sealed record ProcessResult(int ExitCode, string Output);
        public static async Task<ProcessResult> RunProcessAsync(
            string fileName,
            IEnumerable<string> arguments,
            Action<string>? onOutputLine = null,
            CancellationToken token = default)
        {
            ProcessStartInfo startInfo = new(fileName)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process { StartInfo = startInfo };
            var output = new System.Text.StringBuilder();

            void OnData(object sender, DataReceivedEventArgs e)
            {
                if (e.Data == null)
                {
                    return;
                }

                lock (output)
                {
                    output.AppendLine(e.Data);
                }

                onOutputLine?.Invoke(e.Data);
            }

            process.OutputDataReceived += OnData;
            process.ErrorDataReceived += OnData;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(token);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // Already exited.
                }

                throw;
            }

            lock (output)
            {
                return new ProcessResult(process.ExitCode, output.ToString());
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
