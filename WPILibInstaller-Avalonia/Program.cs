using System;
using Avalonia;
using Avalonia.ReactiveUI;
using Spectre.Console;
using WPILibInstaller.CLI;

namespace WPILibInstaller
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            else
            {
                try
                {
                    Spectre.Console.AnsiConsole.MarkupLine("WPILib CLI Installer");
                    new Installer(args).Install().Wait();
                }
                catch (Exception e)
                {
                    AnsiConsole.WriteException(e);
                }
            }
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
