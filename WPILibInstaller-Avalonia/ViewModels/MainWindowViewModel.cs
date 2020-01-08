using Avalonia.Controls;
using DynamicData;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using WPILibInstaller_Avalonia.Models;
using WPILibInstaller_Avalonia.Views;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IScreen
    {
        public RoutingState Router { get; } = new RoutingState();

        public string? ForwardName => (Router.GetCurrentViewModel() as PageViewModelBase)?.ForwardName;

        public string? BackName => (Router.GetCurrentViewModel() as PageViewModelBase)?.BackName;

        public bool ForwardVisible => (Router.GetCurrentViewModel() as PageViewModelBase)?.ForwardVisible ?? false;

        public bool BackVisible => (Router.GetCurrentViewModel() as PageViewModelBase)?.BackVisible ?? false;

        public bool HasSupportFiles
        {
            get => hasSupportFiles;
            set => this.RaiseAndSetIfChanged(ref hasSupportFiles, value);
        }

        private bool hasSupportFiles = false;

        public ReactiveCommand<Unit, IRoutableViewModel> GoNext { get; }

        public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

        private readonly MainWindow mainWindow;
        private readonly StartPageViewModel startPageViewModel;

        public void RefreshForwardBackProperties()
        {
            this.RaisePropertyChanged(nameof(ForwardName));
            this.RaisePropertyChanged(nameof(BackName));
            this.RaisePropertyChanged(nameof(ForwardVisible));
            this.RaisePropertyChanged(nameof(BackVisible));
        }

        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            GoNext = ReactiveCommand.CreateFromObservable(HandleStateChange);

            Router.NavigationChanged.Subscribe((o) =>
            {
                RefreshForwardBackProperties();
            });

            startPageViewModel = new StartPageViewModel(this, mainWindow);
            viewModelStore.Add(typeof(StartPageViewModel), startPageViewModel);

            Router.NavigateAndReset.Execute(startPageViewModel);
        }

        private readonly Dictionary<Type, IRoutableViewModel> viewModelStore = new Dictionary<Type, IRoutableViewModel>();

       
        private IObservable<IRoutableViewModel> HandleStateChange()
        {
            IRoutableViewModel? vm;
            switch (Router.GetCurrentViewModel())
            {
                case StartPageViewModel sp:
                    if (!viewModelStore.TryGetValue(typeof(VSCodePageViewModel), out vm))
                    {
                        vm = new VSCodePageViewModel(this, mainWindow, startPageViewModel.GetVsCodeModel());
                        viewModelStore.Add(typeof(VSCodePageViewModel), vm);
                    }
                    return Router.Navigate.Execute(vm);
                case VSCodePageViewModel vs:
                    if (!viewModelStore.TryGetValue(typeof(ConfigurationPageViewModel), out vm))
                    {
                        vm = new ConfigurationPageViewModel(this);
                        viewModelStore.Add(typeof(ConfigurationPageViewModel), vm);
                    }
                    return Router.Navigate.Execute(vm);
                case ConfigurationPageViewModel cp:
                    if (!viewModelStore.TryGetValue(typeof(InstallPageViewModel), out vm))
                    {
                        vm = new InstallPageViewModel(this);
                        viewModelStore.Add(typeof(InstallPageViewModel), vm);
                    }
                    return Router.NavigateAndReset.Execute(vm);
                case InstallPageViewModel cp:
                    if (!viewModelStore.TryGetValue(typeof(FinalPageViewModel), out vm))
                    {
                        vm = new FinalPageViewModel(this);
                        viewModelStore.Add(typeof(FinalPageViewModel), vm);
                    }
                    return Router.NavigateAndReset.Execute(vm);
                case FinalPageViewModel fp:
                    mainWindow.Close();
                    return Router.CurrentViewModel;
                default:
                    throw new InvalidOperationException("Weird Page?");
            }
        }
    }
}
