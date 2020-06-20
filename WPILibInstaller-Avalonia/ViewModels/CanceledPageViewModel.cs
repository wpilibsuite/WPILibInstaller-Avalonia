using WPILibInstaller_Avalonia.Interfaces;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class CanceledPageViewModel : PageViewModelBase
    {
        private readonly IProgramWindow progWindow;

        public CanceledPageViewModel(IProgramWindow progWindow)
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
