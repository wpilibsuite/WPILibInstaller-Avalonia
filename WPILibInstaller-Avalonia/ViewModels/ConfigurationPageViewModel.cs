using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ReactiveUI;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;

namespace WPILibInstaller.ViewModels
{
    public class ConfigurationPageViewModel : PageViewModelBase, IToInstallProvider
    {
        private readonly IViewModelResolver viewModelResolver;

        public InstallSelectionModel Model { get; } = new InstallSelectionModel();

        public bool InstallVsCode
        {
            get => Model.InstallVsCode;
            set
            {
                Model.InstallVsCode = value;
                Model.InstallVsCodeExtensions = value;
                this.RaisePropertyChanged(nameof(CanInstallExtensions));
                this.RaisePropertyChanged(nameof(InstallVsCode));
            }
        }

        public override bool ForwardVisible => false;

        public bool CanInstallExtensions => vsProvider.Model.AlreadyInstalled || Model.InstallVsCode;

        public bool CanInstallVsCode => vsProvider.Model.ToExtractArchive != null || vsProvider.Model.ToExtractArchiveMacOs != null;

        private readonly IVsCodeInstallLocationProvider vsProvider;

        public bool CanRunAsAdmin => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public ConfigurationPageViewModel(IViewModelResolver viewModelResolver, IVsCodeInstallLocationProvider vsInstallProvider,
            ICatchableButtonFactory buttonFactory)
            : base("Install", "Back")
        {
            this.viewModelResolver = viewModelResolver;
            this.vsProvider = vsInstallProvider;
            InstallLocalUser = buttonFactory.CreateCatchableButton(InstallLocalUserFunc);
            InstallAdmin = buttonFactory.CreateCatchableButton(InstallAdminFunc);
            UpdateVsSettings();
        }

        public ReactiveCommand<Unit, Unit> InstallLocalUser { get; }
        public ReactiveCommand<Unit, Unit> InstallAdmin { get; }

        private async Task InstallLocalUserFunc()
        {
            Model.InstallAsAdmin = false;
            await viewModelResolver.ResolveMainWindow().ExecuteGoNext();
        }

        private async Task InstallAdminFunc()
        {
            Model.InstallAsAdmin = true;
            await viewModelResolver.ResolveMainWindow().ExecuteGoNext();
        }

        public void UpdateVsSettings()
        {
            Model.InstallVsCode = CanInstallVsCode;
            Model.InstallVsCodeExtensions = CanInstallExtensions;
            this.RaisePropertyChanged(nameof(CanInstallExtensions));
            this.RaisePropertyChanged(nameof(InstallVsCode));
            this.RaisePropertyChanged(nameof(CanInstallVsCode));
        }

        public override PageViewModelBase MoveNext()
        {
            return viewModelResolver.Resolve<InstallPageViewModel>();
        }
    }
}
