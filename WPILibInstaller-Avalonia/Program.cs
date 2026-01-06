using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.ReactiveUI;

namespace WPILibInstaller
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static int Main(string[] args)
        {
            // Check for CLI mode
            if (args.Contains("--install") || args.Contains("-i"))
            {
                return RunCliMode(args).GetAwaiter().GetResult();
            }

            // Run GUI mode
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            return 0;
        }

        private static async Task<int> RunCliMode(string[] args)
        {
            bool allUsers = args.Contains("--all-users") || args.Contains("-a");

            var installer = new CliInstaller();
            return await installer.RunInstallAsync(allUsers);
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
