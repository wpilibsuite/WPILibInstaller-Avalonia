using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using WPILibInstaller.Utils;
using WPILibInstaller.ViewModels;
using WPILibInstaller.Views;

namespace WPILibInstaller
{
    public class App : Application
    {
        public override void Initialize()
        {
#if DEBUG
            Console.WriteLine("Debug build - enabling developer tools");
            this.AttachDeveloperTools();
#endif
            // Register our view model locator
            // TODO view model locator

            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var architectureMismatch = PlatformUtils.GetArchitectureMismatchMessage();
                if (architectureMismatch != null)
                {
                    desktop.MainWindow = new MessageDialog(
                        "Wrong Installer Architecture",
                        architectureMismatch,
                        MessageDialogButtons.Ok)
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };
                    base.OnFrameworkInitializationCompleted();
                    return;
                }

                var mainWindow = new MainWindow();
                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
