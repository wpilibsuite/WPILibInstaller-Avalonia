using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using WPILibInstaller_Avalonia.Interfaces;
using WPILibInstaller_Avalonia.Models;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class ConfigurationPageViewModel : PageViewModelBase, IToInstallProvider
    {
        private readonly IDependencyInjection di;

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

        public ConfigurationPageViewModel(IDependencyInjection di, IVsCodeInstallLocationProvider vsInstallProvider)
            : base("Install", "Back")
        {
            this.di = di;
            this.vsProvider = vsInstallProvider;
            UpdateVsSettings();
        }

        public void InstallLocalUser()
        {
            Model.InstallAsAdmin = false;
            di.Resolve<MainWindowViewModel>().GoNext();
        }

        public void InstallAdmin()
        {
            Model.InstallAsAdmin = true;
            di.Resolve<MainWindowViewModel>().GoNext();
        }

        public void UpdateVsSettings()
        {
            vsAlreadyInstalled = vsProvider.AlreadyInstalled;
            CanInstallVsCode = vsProvider.Model.ToExtractArchive != null;
            Model.InstallVsCode = CanInstallVsCode;
            this.RaisePropertyChanged(nameof(CanInstallExtensions));
            this.RaisePropertyChanged(nameof(InstallVsCode));
            this.RaisePropertyChanged(nameof(CanInstallVsCode));
            this.RaisePropertyChanged(nameof(InstallVsCodeExtensions));
        }

        public override PageViewModelBase MoveNext()
        {
            return di.Resolve<InstallPageViewModel>();
        }
    }
}
