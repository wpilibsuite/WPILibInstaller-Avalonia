using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ReactiveUI;
using WPILibInstaller.Interfaces;

namespace WPILibInstaller.ViewModels
{
    public class FinalPageViewModel : PageViewModelBase
    {
        private readonly IProgramWindow progWindow;
        private readonly IConfigurationProvider configurationProvider;
        private readonly bool vsCodeInstalled;

        public string FinishText { get; }

        public ReactiveCommand<Unit, Unit> OpenKnownIssues { get; }

        public ReactiveCommand<Unit, Unit> OpenChangelog { get; }

        public ReactiveCommand<Unit, Unit> OpenBetaSite { get; }

        public ReactiveCommand<Unit, Unit> OpenBetaDocs { get; }

        public FinalPageViewModel(IProgramWindow progWindow, IConfigurationProvider configurationProvider, IVsCodeInstallLocationProvider vsCodeProvider,
                ICatchableButtonFactory buttonFactory)
            : base("Finish", "")
        {
            vsCodeInstalled = vsCodeProvider.Model.InstallingVsCode;
            if (vsCodeInstalled)
            {
                if (OperatingSystem.IsMacOS())
                {
                    FinishText = "Clicking finish will open the VS Code folder.\nPlease drag this to your dock.";
                }
                else
                {
                    FinishText = "Use the WPILib VS Code desktop icon to start VS Code.";
                }
            }
            else
            {
                FinishText = "";
            }

            OpenKnownIssues = buttonFactory.CreateCatchableButton(OpenKnownIssuesFunc);
            OpenChangelog = buttonFactory.CreateCatchableButton(OpenChangelogFunc);
            OpenBetaDocs = buttonFactory.CreateCatchableButton(OpenBetaDocsFunc);
            OpenBetaSite = buttonFactory.CreateCatchableButton(OpenBetaSiteFunc);

            this.progWindow = progWindow;
            this.configurationProvider = configurationProvider;
        }

        public Task OpenKnownIssuesFunc()
        {
            OpenBrowser("https://docs.wpilib.org/en/stable/docs/yearly-overview/known-issues.html");
            return Task.CompletedTask;
        }

        public Task OpenBetaDocsFunc()
        {
            OpenBrowser("https://docs.wpilib.org/en/latest/docs/beta/beta-getting-started/welcome.html");
            return Task.CompletedTask;
        }

        public Task OpenBetaSiteFunc()
        {
            OpenBrowser("https://github.com/wpilibsuite/2026Beta");
            return Task.CompletedTask;
        }

        public Task OpenChangelogFunc()
        {
            string? verString = null;
            try
            {
                var baseDir = AppContext.BaseDirectory;
                verString = File.ReadAllText(Path.Join(baseDir, "WPILibInstallerVersion.txt")).Trim();
            }
            catch
            {
            }

            if (verString != null)
            {
                OpenBrowser($"https://github.com/wpilibsuite/allwpilib/releases/tag/v{verString}");
            }
            else
            {
                OpenBrowser($"https://github.com/wpilibsuite/allwpilib/releases/");
            }


            return Task.CompletedTask;
        }

        public override PageViewModelBase MoveNext()
        {
            if (vsCodeInstalled && OperatingSystem.IsMacOS())
            {
                ProcessStartInfo pstart = new ProcessStartInfo("open", Path.Join(configurationProvider.InstallDirectory, "vscode"));
                var p = Process.Start(pstart);
                if (p != null)
                {
                    p.WaitForExit();
                }
            }
            progWindow.CloseProgram();
            return this;
        }

        // Borrowed under MIT license from
        // https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Dialogs/AboutAvaloniaDialog.xaml.cs

        public static void OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // If no associated application/json MimeType is found xdg-open opens retrun error
                // but it tries to open it anyway using the console editor (nano, vim, other..)
                ShellExec($"xdg-open {url}");
            }
            else
            {
                using Process? process = Process.Start(new ProcessStartInfo
                {
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? url : "open",
                    Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"{url}" : "",
                    CreateNoWindow = true,
                    UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                });
            }
        }

        private static void ShellExec(string cmd)
        {
            var escapedArgs = Regex.Replace(cmd, "(?=[`~!#&*()|;'<>])", "\\")
                .Replace("\"", "\\\\\\\"");

            using var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            );
        }
    }
}
