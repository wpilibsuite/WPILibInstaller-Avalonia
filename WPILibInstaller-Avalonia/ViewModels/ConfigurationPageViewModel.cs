using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using WPILibInstaller_Avalonia.Interfaces;
using WPILibInstaller_Avalonia.Models;

namespace WPILibInstaller_Avalonia.ViewModels
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

        public bool InstallVsCodeExtensions
        {
            get => Model.InstallVsCodeExtensions;
            set
            {
                Model.InstallVsCodeExtensions = value;
                this.RaisePropertyChanged();
            }
        }


        public bool CanInstallExtensions
        {
            get => vsAlreadyInstalled || Model.InstallVsCode;
        }

        public bool CanInstallVsCode { get; private set; }

        private bool vsAlreadyInstalled;

        private readonly IVsCodeInstallLocationProvider vsProvider;

        private bool canRunAsAdmin = true;

        public bool CanRunAsAdmin
        {
            get => canRunAsAdmin;
            set => this.RaiseAndSetIfChanged(ref canRunAsAdmin, value);
        }

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
            vsAlreadyInstalled = vsProvider.Model.AlreadyInstalled;
            CanInstallVsCode = vsProvider.Model.ToExtractArchive != null;
            Model.InstallVsCode = CanInstallVsCode;
            Model.InstallVsCodeExtensions = CanInstallExtensions;
            this.RaisePropertyChanged(nameof(CanInstallExtensions));
            this.RaisePropertyChanged(nameof(InstallVsCode));
            this.RaisePropertyChanged(nameof(CanInstallVsCode));
            this.RaisePropertyChanged(nameof(InstallVsCodeExtensions));
        }

        public override PageViewModelBase MoveNext()
        {
            return viewModelResolver.Resolve<InstallPageViewModel>();
        }
    }
}
