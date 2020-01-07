using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using WPILibInstaller_Avalonia.Views;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IScreen
    {
        public RoutingState Router { get; } = new RoutingState();

        public ReactiveCommand<Unit, IRoutableViewModel> GoNext { get; }

        public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

        public MainWindow MainWindow { get; set; }

        public MainWindowViewModel()
        {
            GoNext = ReactiveCommand.CreateFromObservable(HandleStateChange);

            var startvm = new StartPageViewModel(this);
            viewModelStore.Add(typeof(StartPageViewModel), startvm);

            Router.NavigateAndReset.Execute(startvm);
        }

        private Dictionary<Type, IRoutableViewModel> viewModelStore = new Dictionary<Type, IRoutableViewModel>();

        private IObservable<IRoutableViewModel> HandleStateChange()
        {
            IRoutableViewModel vm = null;
            switch (Router.GetCurrentViewModel())
            {
                case StartPageViewModel sp:
                    if (!viewModelStore.TryGetValue(typeof(VSCodePageViewModel), out vm))
                    {
                        vm = new VSCodePageViewModel(this);
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
                    MainWindow.Close();
                    return Router.CurrentViewModel;
                default:
                    throw new InvalidOperationException("Weird Page?");
            }
        }
    }
}
