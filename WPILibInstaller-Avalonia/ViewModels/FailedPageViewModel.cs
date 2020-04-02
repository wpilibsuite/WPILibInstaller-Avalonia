using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;
using WPILibInstaller_Avalonia.Interfaces;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class FailedPageViewModel : PageViewModelBase
    {
        private readonly IProgramWindow progWindow;

        private Exception? canceledByException = null;

        public string ExceptionText => canceledByException?.ToString() ?? "";

        public void SetException(Exception ex)
        {
            canceledByException = ex;
            this.RaisePropertyChanged(nameof(ExceptionText));
        }

        public FailedPageViewModel(IProgramWindow progWindow)
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
