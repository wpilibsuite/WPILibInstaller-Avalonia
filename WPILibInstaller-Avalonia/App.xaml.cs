using Autofac;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using Splat;
using System.Reflection;
using WPILibInstaller_Avalonia.ViewModels;
using WPILibInstaller_Avalonia.Views;

namespace WPILibInstaller_Avalonia
{
    public class App : Application
    {
        public override void Initialize()
        {
            // Register our view model locator
            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());


            // Register our DI
            ContainerBuilder builder = new ContainerBuilder();

            builder.RegisterType<StartPageViewModel>();

            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {

                var mainWindow = new MainWindow();
                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
