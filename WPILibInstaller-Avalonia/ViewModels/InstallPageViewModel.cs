using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WPILibInstaller_Avalonia.Interfaces;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class InstallPageViewModel : PageViewModelBase
    {
        private readonly IDependencyInjection di;
        private readonly IToInstallProvider toInstallProvider;

        private string text = "Starting";
        private int progress = 0;

        public int Progress { get => progress; set => this.RaiseAndSetIfChanged(ref progress, value); }
        public string Text { get => text; set => this.RaiseAndSetIfChanged(ref text, value); }

        public InstallPageViewModel(IScreen screen, IDependencyInjection di, IToInstallProvider toInstallProvider)
            : base("", "", "Starting", screen)
        {
            this.di = di;
            this.toInstallProvider = toInstallProvider;
            _ = RunInstall();
        }

        private async Task RunInstall()
        {
            for (int i = 0; i < 100; i++)
            {
                await Task.Delay(100);
                Progress = i;
            }
            MoveNext();
        }

        public override IObservable<IRoutableViewModel> MoveNext()
        {
            return MoveNext(di.Resolve<FinalPageViewModel>());
        }
    }
}
