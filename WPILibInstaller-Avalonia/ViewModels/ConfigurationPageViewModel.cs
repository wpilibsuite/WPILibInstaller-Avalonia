using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;

namespace WPILibInstaller.ViewModels
{
    public class ConfigurationPageViewModel : PageViewModelBase, IToInstallProvider
    {
        private readonly IViewModelResolver viewModelResolver;

        public InstallSelectionModel Model { get; } = new InstallSelectionModel();

        public override bool ForwardVisible => false;

        public bool CanRunAsAdmin => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public ConfigurationPageViewModel(IViewModelResolver viewModelResolver, ICatchableButtonFactory buttonFactory)
            : base("Install", "Back")
        {
            this.viewModelResolver = viewModelResolver;
            InstallLocalUser = buttonFactory.CreateCatchableButton(InstallLocalUserFunc);
            InstallAdmin = buttonFactory.CreateCatchableButton(InstallAdminFunc);
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

        public override PageViewModelBase MoveNext()
        {
            if (Model.InstallTools)
            {
                return viewModelResolver.Resolve<InstallPageViewModel>();
            }
            else
            {
                return viewModelResolver.Resolve<VSCodePageViewModel>();
            }
        }
    }
}
