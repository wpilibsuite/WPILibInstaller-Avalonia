using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using WPILibInstaller_Avalonia.Interfaces;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class CanceledPageViewModel : PageViewModelBase
    {
        private readonly IProgramWindow progWindow;

        public CanceledPageViewModel(IScreen screen, IProgramWindow progWindow)
            : base("Finish", "", "canceled", screen)
        {
            this.progWindow = progWindow;
        }

        public override IObservable<IRoutableViewModel> MoveNext()
        {
            progWindow.CloseProgram();
            return HostScreen.Router.CurrentViewModel;
        }
    }
}
