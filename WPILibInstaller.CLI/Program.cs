using System;
using System.Linq;
using System.Threading.Tasks;

namespace WPILibInstaller.CLI
{
    class Program
    {
        public static int Main(string[] args)
        {
            // Check for help
            if (args.Contains("--help") || args.Contains("-h"))
            {
                PrintHelp();
                return 0;
            }

            return RunCliMode(args).GetAwaiter().GetResult();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("WPILib Installer - CLI");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  WPILibInstaller-CLI [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -a, --all-users            Install for all users (requires admin/sudo)");
            Console.WriteLine("  --install-mode <mode>      Installation mode: 'all' or 'tools' (default: all)");
            Console.WriteLine("                             - all:   Full installation with VS Code");
            Console.WriteLine("                             - tools: Tools only (JDK + WPILib tools)");
            Console.WriteLine("  -h, --help                 Show this help message");
            Console.WriteLine();
            Console.WriteLine("Offline Installation:");
            Console.WriteLine("  If VS Code download fails, the installer will automatically check for an");
            Console.WriteLine("  offline archive named 'vscode.zip' or 'vscode.tar.gz' in the same directory.");
            Console.WriteLine("  You can pre-download VS Code and save it with this name for offline installs.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  WPILibInstaller-CLI");
            Console.WriteLine("  WPILibInstaller-CLI --all-users");
            Console.WriteLine("  WPILibInstaller-CLI --install-mode tools");
            Console.WriteLine();
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
    }
}
