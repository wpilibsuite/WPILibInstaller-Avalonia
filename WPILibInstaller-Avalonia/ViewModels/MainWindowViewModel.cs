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

        public MainWindow? MainWindow { get; set; }

        public void RefreshForwardBackProperties()
        {
            this.RaisePropertyChanged(nameof(ForwardName));
            this.RaisePropertyChanged(nameof(BackName));
            this.RaisePropertyChanged(nameof(ForwardVisible));
            this.RaisePropertyChanged(nameof(BackVisible));
        }

        public MainWindowViewModel()
        {
            GoNext = ReactiveCommand.CreateFromObservable(HandleStateChange);

            Router.NavigationChanged.Subscribe((o) =>
            {
                RefreshForwardBackProperties();
            });

            var startvm = new StartPageViewModel(this);
            viewModelStore.Add(typeof(StartPageViewModel), startvm);

            Router.NavigateAndReset.Execute(startvm);
        }

        private readonly Dictionary<Type, IRoutableViewModel> viewModelStore = new Dictionary<Type, IRoutableViewModel>();

        public VSCodeModel GetVSCodeModel()
        {
            VSCodeModel model = new VSCodeModel("1.41.1");
            model.Platforms.Add(Utils.Platform.Win64, new VSCodeModel.PlatformData("https://vscode-update.azurewebsites.net/1.41.1/win32-x64-archive/stable", ""));
            return model;
        }

        private ZipArchive filesArchive;
        private VsCodeConfig vscodeConfig;
        private UpgradeConfig upgradeConfig;
        private ToolConfig toolConfig;
        private JdkConfig jdkConfig;
        private FullConfig fullConfig;

        public async Task SelectSupportFiles()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AllowMultiple = false,
            };

            var selectedFiles = await openFileDialog.ShowAsync(MainWindow!);
            if (selectedFiles.Length != 1) return;
            var file = selectedFiles[0];

            var stream = new FileStream(file, FileMode.Open);
            filesArchive = new ZipArchive(stream, ZipArchiveMode.Read);

            var vscodeEntry = filesArchive.GetEntry("installUtils/vscodeConfig.json");

            using (StreamReader reader = new StreamReader(vscodeEntry.Open()))
            {
                var vsConfigStr = await reader.ReadToEndAsync();
                vscodeConfig = JsonConvert.DeserializeObject<VsCodeConfig>(vsConfigStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                });
            }

        }

        private IObservable<IRoutableViewModel> HandleStateChange()
        {
            IRoutableViewModel? vm;
            switch (Router.GetCurrentViewModel())
            {
                case StartPageViewModel sp:
                    if (!viewModelStore.TryGetValue(typeof(VSCodePageViewModel), out vm))
                    {
                        vm = new VSCodePageViewModel(this, GetVSCodeModel());
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
                    MainWindow!.Close();
                    return Router.CurrentViewModel;
                default:
                    throw new InvalidOperationException("Weird Page?");
            }
        }
    }
}
