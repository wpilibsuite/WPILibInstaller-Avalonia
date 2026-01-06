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
            // Check for help
            if (args.Contains("--help") || args.Contains("-h"))
            {
                PrintHelp();
                return 0;
            }

            // Check for CLI mode
            if (args.Contains("--install") || args.Contains("-i"))
            {
                return RunCliMode(args).GetAwaiter().GetResult();
            }

            // Run GUI mode
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            return 0;
        }

        private static void PrintHelp()
        {
            System.Console.WriteLine("WPILib Installer");
            System.Console.WriteLine();
            System.Console.WriteLine("Usage:");
            System.Console.WriteLine("  WPILibInstaller [options]");
            System.Console.WriteLine();
            System.Console.WriteLine("Options:");
            System.Console.WriteLine("  -i, --install              Run in CLI installation mode");
            System.Console.WriteLine("  -a, --all-users            Install for all users (requires admin/sudo)");
            System.Console.WriteLine("  --install-mode <mode>      Installation mode: 'all' or 'tools' (default: all)");
            System.Console.WriteLine("                             - all:   Full installation with VS Code");
            System.Console.WriteLine("                             - tools: Tools only (JDK + WPILib tools)");
            System.Console.WriteLine("  -h, --help                 Show this help message");
            System.Console.WriteLine();
            System.Console.WriteLine("Examples:");
            System.Console.WriteLine("  WPILibInstaller --install");
            System.Console.WriteLine("  WPILibInstaller --install --all-users");
            System.Console.WriteLine("  WPILibInstaller --install --install-mode tools");
            System.Console.WriteLine();
        }

        private static async Task<int> RunCliMode(string[] args)
        {
            bool allUsers = args.Contains("--all-users") || args.Contains("-a");

            // Parse install mode (default to "all")
            string installMode = "all";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--install-mode" && i + 1 < args.Length)
                {
                    installMode = args[i + 1].ToLowerInvariant();
                    break;
                }
            }

            var installer = new CliInstaller();
            return await installer.RunInstallAsync(allUsers, installMode);
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
