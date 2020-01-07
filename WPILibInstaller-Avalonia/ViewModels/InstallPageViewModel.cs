using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class InstallPageViewModel : PageViewModelBase, IRoutableViewModel
    {
        public IScreen HostScreen => mainPage;

        private readonly MainWindowViewModel mainPage;

        public int Progress { get; set; } = 0;

        public string Text { get; set; } = "Starting";

        public string UrlPathSegment { get; } = "Install";

        public InstallPageViewModel(MainWindowViewModel screen)
            : base("", "")
        {
            mainPage = screen;
            _ = RunInstall();
        }

        private async Task RunInstall()
        {
            await Task.Delay(5000);
            mainPage.GoNext.Execute();
        }
    }
}
