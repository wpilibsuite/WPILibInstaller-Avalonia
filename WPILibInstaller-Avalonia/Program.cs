﻿using Avalonia;
using Avalonia.ReactiveUI;
using WPILibInstaller.CLI;
using System;

namespace WPILibInstaller
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            Console.WriteLine(args.Length);
            if (args.Length == 0)
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            else
            {
                Console.WriteLine("Installing with CLI");
                try
                {
                    new Installer(args).Install().Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine("CLI Installation Failed: " + e.Message);
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
