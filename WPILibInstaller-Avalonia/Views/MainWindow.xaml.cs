using Autofac;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using MessageBox.Avalonia;
using ReactiveUI;
using Splat;
using System;
using System.Threading.Tasks;
using WPILibInstaller_Avalonia.Interfaces;
using WPILibInstaller_Avalonia.Utils;
using WPILibInstaller_Avalonia.ViewModels;

namespace WPILibInstaller_Avalonia.Views
{
    public class MainWindow : ReactiveWindow<MainWindowViewModel>, IProgramWindow, IViewModelResolver
    {
        public IContainer Container { get; }

        public Window Window => this;

        public MainWindow()
        {
            // Initialize our DI
            ContainerBuilder builder = new ContainerBuilder();

            builder.RegisterAssemblyTypes(typeof(MainWindow).Assembly).Where(x => x.IsClass && x.Name.EndsWith("ViewModel")).SingleInstance().AsSelf().AsImplementedInterfaces();
            builder.RegisterInstance(this).AsImplementedInterfaces();

            Container = builder.Build();

            ViewModel = Container.Resolve<MainWindowViewModel>();
            ViewModel.Initialize();

            DataContext = ViewModel;

            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public void CloseProgram()
        {
            this.Close();
        }

        public async Task<string?> ShowFilePicker(string title, string? initialiDirectory)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Title = title,
            };
            if (initialiDirectory != null)
            {
                dialog.Directory = initialiDirectory;
            }
            var result = await dialog.ShowAsync(this);
            if (result == null) return null;
            if (result.Length != 1) return null;
            return result[0];
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
            return ViewModel;
        }
    }
}
