using Avalonia;
using Avalonia.ReactiveUI;
using WPILibInstaller.CLI;

namespace WPILibInstaller
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args) {
            if (args.Length == 0)
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            else new Installer(args).Install();
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
        }
    }
}
