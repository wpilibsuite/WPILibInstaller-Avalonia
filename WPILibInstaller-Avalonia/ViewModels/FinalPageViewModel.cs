using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;
using WPILibInstaller_Avalonia.Interfaces;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class FinalPageViewModel : PageViewModelBase
    {
        private readonly IProgramWindow progWindow;

        public FinalPageViewModel(IScreen screen, IProgramWindow progWindow)
            : base("Finish", "", "finish", screen)
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
