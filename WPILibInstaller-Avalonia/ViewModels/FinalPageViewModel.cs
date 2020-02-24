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

        public FinalPageViewModel(IProgramWindow progWindow)
            : base("Finish", "")
        {
            this.progWindow = progWindow;
        }

        public override PageViewModelBase MoveNext()
        {
            progWindow.CloseProgram();
            return this;
        }
    }
}
