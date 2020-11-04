using Autofac;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using System.Threading.Tasks;
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

            builder.RegisterAssemblyTypes(typeof(MainWindow).Assembly).Where(x => x.IsClass && x.Name.EndsWith("ViewModel")).SingleInstance().AsSelf().AsImplementedInterfaces();
            builder.RegisterInstance(this).AsImplementedInterfaces();

            Container = builder.Build();

            ViewModel = Container.Resolve<MainWindowViewModel>();
            ViewModel.Initialize();

            DataContext = ViewModel;

            InitializeComponent();
        }

        public void CloseProgram()
        {
            this.Close();
        }

        public async Task<string?> ShowFilePicker(string title, string? initialiDirectory, string extensionFilter)
        {

            OpenFileDialog dialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Title = title,
                Filters  = new List<FileDialogFilter>() { 
                    new FileDialogFilter {
                        Name = "ZIP Archive",
                        Extensions = new List<string>() {
                            extensionFilter,
                        }
                    },
                },
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

        public async Task<string?> ShowFolderPicker(string title, string? initialiDirectory)
        {

            OpenFolderDialog dialog = new OpenFolderDialog
            {
                Title = title,
            };
            if (initialiDirectory != null)
            {
                dialog.Directory = initialiDirectory;
            }
            var result = await dialog.ShowAsync(this);
            if (string.IsNullOrWhiteSpace(result)) return null;
            return result;
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
