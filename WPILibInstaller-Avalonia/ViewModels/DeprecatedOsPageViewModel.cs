using WPILibInstaller.Interfaces;

namespace WPILibInstaller.ViewModels
{
    public class DeprecatedOsPageViewModel : PageViewModelBase
    {
        private readonly IViewModelResolver viewModelResolver;

        public DeprecatedOsPageViewModel(IViewModelResolver viewModelResolver)
            : base("I Understand", "")
        {
            this.viewModelResolver = viewModelResolver;
            var version = Environment.OSVersion;
            var bitness = IntPtr.Size == 8 ? "64 Bit" : "32 Bit";
            CurrentSystem = $"Detected {version.VersionString} {bitness}";
        }

        public string CurrentSystem { get; }

        public override PageViewModelBase MoveNext()
        {
            return viewModelResolver.Resolve<StartPageViewModel>();
        }
    }
}
