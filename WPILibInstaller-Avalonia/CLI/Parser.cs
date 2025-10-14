using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPILibInstaller.Models;
using WPILibInstaller.Models.CLI;

namespace WPILibInstaller.CLI
{
    class Parser
    {
        public readonly CLIConfigurationProvider configurationProvider;

        public readonly CLIInstallSelectionModel installSelectionModel;

        public Parser(string[] args)
        {
            string artifactsFile = "", resourcesFile = "";
            installSelectionModel = new CLIInstallSelectionModel();

            bool skip = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--help" || args[i] == "-h")
                {
                    Console.WriteLine("Copyright (c) FIRST and other WPILib contributors.\n");
                    Console.WriteLine("The following options are available:");
                    Console.WriteLine("  -a,--artifacts                The artifacts file to use for installation");
                    Console.WriteLine("  -r,--resources                The resources file to use for installation");
                    Console.WriteLine("  -h,--help                     Show this help message");
                    Console.WriteLine("  --as-admin                    Install WPILib as an administrator");
                    Console.WriteLine("  --without-vscode              Do not install Visual Studio Code");
                    Console.WriteLine("  --without-gradle              Do not install Gradle");
                    Console.WriteLine("  --without-jdk                 Do not install the Java Development Kit");
                    Console.WriteLine("  --without-tools               Do not install the WPILib tools");
                    Console.WriteLine("  --without-wpilibdeps          Do not install the WPILib dependencies");
                    Console.WriteLine("  --without-vscodeextensions    Do not install the Visual Studio Code extensions");

                    throw new Exception("Couldn't create parser - only showing help message");
                }
                if (skip)
                {
                    skip = false;
                    continue;
                }
                if (args[i] == "--artifacts" || args[i] == "-a")
                {
                    artifactsFile = args[i + 1];
                    skip = true;
                }
                if (args[i] == "--resources" || args[i] == "-r")
                {
                    resourcesFile = args[i + 1];
                    skip = true;
                }
                if (args[i] == "--as-admin")
                {
                    installSelectionModel.InstallAsAdmin = true;
                }
                if (args[i] == "--without-vscode")
                {
                    installSelectionModel.InstallVsCode = false;
                }
                if (args[i] == "--without-gradle")
                {
                    installSelectionModel.InstallGradle = false;
                }
                if (args[i] == "--without-jdk")
                {
                    installSelectionModel.InstallJDK = false;
                }
                if (args[i] == "--without-tools")
                {
                    installSelectionModel.InstallTools = false;
                }
                if (args[i] == "--without-wpilibdeps")
                {
                    installSelectionModel.InstallWPILibDeps = false;
                }
                if (args[i] == "--without-vscodeextensions")
                {
                    installSelectionModel.InstallVsCodeExtensions = false;
                }
            }

            var task = CLIConfigurationProvider.From(artifactsFile, resourcesFile);
            task.Wait();
            configurationProvider = task.Result;
        }
    }
}
