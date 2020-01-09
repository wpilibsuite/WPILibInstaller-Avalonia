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
using WPILibInstaller_Avalonia.Interfaces;
using WPILibInstaller_Avalonia.Models;
using WPILibInstaller_Avalonia.Views;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IScreen, IMainWindowViewModelRefresher
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

        public void RefreshForwardBackProperties()
        {
            this.RaisePropertyChanged(nameof(ForwardName));
            this.RaisePropertyChanged(nameof(BackName));
            this.RaisePropertyChanged(nameof(ForwardVisible));
            this.RaisePropertyChanged(nameof(BackVisible));
        }

        private readonly IDependencyInjection di;

        public MainWindowViewModel(IDependencyInjection di)
        {
            GoNext = ReactiveCommand.CreateFromObservable(HandleStateChange);

            Router.NavigationChanged.Subscribe((o) =>
            {
                RefreshForwardBackProperties();
            });

            this.di = di;
        }

        public void Initialize()
        {
            Router.NavigateAndReset.Execute(di.Resolve<StartPageViewModel>());
        }
       
        private IObservable<IRoutableViewModel> HandleStateChange()
        {
            var model = (PageViewModelBase)Router.GetCurrentViewModel();
            return model.MoveNext();
        }
    }
}
