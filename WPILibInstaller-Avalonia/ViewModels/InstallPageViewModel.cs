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

        private string text = "Starting";
        private int progress = 0;

        public int Progress { get => progress; set => this.RaiseAndSetIfChanged(ref progress, value); }
        public string Text { get => text; set => this.RaiseAndSetIfChanged(ref text, value); }

        public InstallPageViewModel(IScreen screen, IDependencyInjection di)
            : base("", "", "Starting", screen)
        {
            this.di = di;
            _ = RunInstall();
        }

        private async Task RunInstall()
        {
            await Task.Delay(5000);
            MoveNext();
        }

        public override IObservable<IRoutableViewModel> MoveNext()
        {
            return MoveNext(di.Resolve<FinalPageViewModel>());
        }
    }
}
