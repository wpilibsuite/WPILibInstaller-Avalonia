using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Platform.Storage;
using WPILibInstaller.Interfaces;
using WPILibInstaller.ViewModels;

namespace WPILibInstaller.Views
{
    public class MainWindow : ReactiveWindow<MainWindowViewModel>, IProgramWindow, IViewModelResolver
    {
        public IContainer Container { get; }

        public Window Window => this;

        public MainWindow()
        {
            // Initialize our DI
            ContainerBuilder builder = new ContainerBuilder();

            builder.RegisterType<CanceledPageViewModel>().SingleInstance().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<ConfigurationPageViewModel>().SingleInstance().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<FailedPageViewModel>().SingleInstance().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<FinalPageViewModel>().SingleInstance().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<InstallPageViewModel>().SingleInstance().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<MainWindowViewModel>().SingleInstance().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<StartPageViewModel>().SingleInstance().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<VSCodePageViewModel>().SingleInstance().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<DeprecatedOsPageViewModel>().SingleInstance().AsSelf().AsImplementedInterfaces();
            builder.RegisterInstance(this).AsImplementedInterfaces();

            Container = builder.Build();

            ViewModel = Container.Resolve<MainWindowViewModel>();
            DataContext = ViewModel;
            ViewModel.Initialize();

            InitializeComponent();
        }

        public void CloseProgram()
        {
            this.Close();
        }

        public async Task<string?> ShowFilePicker(string title, string extensionFilter, string? initialiDirectory)
        {
            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("Archive")
                    {
                        Patterns = new[] { $"*.{extensionFilter}" }
                    }
                }
            };

            if (initialiDirectory != null)
            {
                options.SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(initialiDirectory);
            }

            var result = await StorageProvider.OpenFilePickerAsync(options);
            if (result == null || result.Count != 1) return null;
            return result[0].Path.LocalPath;
        }

        public async Task<string?> ShowFolderPicker(string title, string? initialiDirectory)
        {
            var options = new FolderPickerOpenOptions
            {
                Title = title
            };

            if (initialiDirectory != null)
            {
                options.SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(initialiDirectory);
            }

            var result = await StorageProvider.OpenFolderPickerAsync(options);
            if (result == null || result.Count != 1) return null;
            return result[0].Path.LocalPath;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public T Resolve<T>() where T : notnull, PageViewModelBase
        {
            return Container.Resolve<T>();
        }

        public IMainWindowViewModel ResolveMainWindow()
        {
            return ViewModel!;
        }
    }
}
