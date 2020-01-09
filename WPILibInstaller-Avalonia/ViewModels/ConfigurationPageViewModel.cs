using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using WPILibInstaller_Avalonia.Interfaces;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class ConfigurationPageViewModel : PageViewModelBase
    {
        private readonly IDependencyInjection di;

        public ConfigurationPageViewModel(IScreen screen, IDependencyInjection di)
            : base("Install", "Back", "Configuration", screen)
        {
            this.di = di;
        }

        public override IObservable<IRoutableViewModel> MoveNext()
        {
            return MoveNext(di.Resolve<InstallPageViewModel>());
        }
    }
}
