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
        private string text = "Starting";
        private int progress = 0;

        public int Progress { get => progress; set => this.RaiseAndSetIfChanged(ref progress, value); }
        public string Text { get => text; set => this.RaiseAndSetIfChanged(ref text, value); }
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
