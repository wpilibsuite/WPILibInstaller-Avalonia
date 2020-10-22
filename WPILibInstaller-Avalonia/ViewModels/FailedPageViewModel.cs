using System;
using ReactiveUI;
using WPILibInstaller.Interfaces;

namespace WPILibInstaller.ViewModels
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
