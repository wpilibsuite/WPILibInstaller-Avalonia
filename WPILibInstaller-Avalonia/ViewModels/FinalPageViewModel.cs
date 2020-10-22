using WPILibInstaller.Interfaces;

namespace WPILibInstaller.ViewModels
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
