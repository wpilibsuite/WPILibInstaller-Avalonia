using System;
using System.Diagnostics;
using System.IO;
using WPILibInstaller.Interfaces;

namespace WPILibInstaller.ViewModels
{
    public class FinalPageViewModel : PageViewModelBase
    {
        private readonly IProgramWindow progWindow;
        private readonly IConfigurationProvider configurationProvider;
        private readonly bool vsCodeInstalled;

        public string FinishText { get; }

        public FinalPageViewModel(IProgramWindow progWindow, IConfigurationProvider configurationProvider, IToInstallProvider toInstallProvider)
            : base("Finish", "")
        {
            vsCodeInstalled = toInstallProvider.Model.InstallVsCode;
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

            this.progWindow = progWindow;
            this.configurationProvider = configurationProvider;
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
    }
}
