using System.CommandLine;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.CLI
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var architectureMismatch = PlatformUtils.GetArchitectureMismatchMessage();
            if (architectureMismatch != null)
            {
                Console.Error.WriteLine($"Error: {architectureMismatch}");
                return 1;
            }

            var allUsersOption = new Option<bool>("--all-users")
            {
                Description = "Create Windows shortcuts with elevation when needed"
            };
            allUsersOption.Aliases.Add("-a");

            var installJavaOption = new Option<bool>("--install-java")
            {
                Description = "Install Java support"
            };
            var installCppOption = new Option<bool>("--install-cpp")
            {
                Description = "Install C++ support"
            };
            var installPythonOption = new Option<bool>("--install-python")
            {
                Description = "Install Python support (not yet available)"
            };
            var installLabViewOption = new Option<bool>("--install-labview")
            {
                Description = "Install LabVIEW add-ons (not yet available)"
            };
            var installWpilibToolsOption = new Option<bool>("--install-tools")
            {
                Description = "Install WPILib tools (Glass, SysId, OutlineViewer, Data Log Tool, WPIcal)"
            };
            var installElasticOption = new Option<bool>("--install-elastic")
            {
                Description = "Install Elastic dashboard"
            };
            var installAdvantageScopeOption = new Option<bool>("--install-advantagescope")
            {
                Description = "Install AdvantageScope"
            };

            var resourcesOption = new Option<string?>("--resources")
            {
                Description = "Resource ZIP to use instead of auto-detecting",
                Hidden = true
            };
            resourcesOption.Aliases.Add("--resource-file");
            resourcesOption.Aliases.Add("--resources-file");

            var artifactsOption = new Option<string?>("--artifacts")
            {
                Description = "Artifacts archive to use instead of auto-detecting",
                Hidden = true
            };
            artifactsOption.Aliases.Add("--artifact-file");
            artifactsOption.Aliases.Add("--artifacts-file");

            var vsCodeOption = new Option<string?>("--vscode")
            {
                Description = "Predownloaded VS Code archive to install"
            };
            vsCodeOption.Aliases.Add("--vscode-archive");
            vsCodeOption.Aliases.Add("--vs-code-archive");

            var forceOption = new Option<bool>("--force")
            {
                Description = "Skip the confirmation prompt and start installation"
            };
            forceOption.Aliases.Add("-y");
            forceOption.Aliases.Add("--yes");

            RootCommand rootCommand = new("WPILib Installer - CLI")
            {
                allUsersOption,
                installJavaOption,
                installCppOption,
                installPythonOption,
                installLabViewOption,
                installWpilibToolsOption,
                installElasticOption,
                installAdvantageScopeOption,
                resourcesOption,
                artifactsOption,
                vsCodeOption,
                forceOption
            };

            rootCommand.Description = """
                Installer files:
                  Auto-detection uses WPILibInstallerVersion.txt and searches the same bundled installer locations as the GUI.

                Components:
                  With no --install-* flags, all available components are installed. Supplying any
                  --install-* flag installs only the components whose flags are set.
                  --install-tools means WPILib Tools only; use --install-elastic and
                  --install-advantagescope for the other dashboards.
                  Python and LabVIEW flags are reserved while those installers are in progress.

                Offline VS Code:
                  If the VS Code download fails, the CLI checks the installer directory, current directory, and macOS installer volume for a matching archive.
                """;

            rootCommand.SetAction(async parseResult =>
            {
                var allUsers = parseResult.GetValue(allUsersOption);
                var resourcesFile = parseResult.GetValue(resourcesOption);
                var artifactsFile = parseResult.GetValue(artifactsOption);
                var vsCodeArchive = parseResult.GetValue(vsCodeOption);
                var force = parseResult.GetValue(forceOption);

                Option<bool>[] componentOptions =
                [
                    installJavaOption,
                    installCppOption,
                    installPythonOption,
                    installLabViewOption,
                    installWpilibToolsOption,
                    installElasticOption,
                    installAdvantageScopeOption,
                ];
                bool componentsSpecified = componentOptions.Any(option => parseResult.GetResult(option) != null);

                // With no component flags, keep the default set of all currently available
                // components. As soon as any flag is supplied, the flags are authoritative.
                var selection = new InstallSelectionModel();
                if (componentsSpecified)
                {
                    selection.InstallJava = parseResult.GetValue(installJavaOption);
                    selection.InstallCpp = parseResult.GetValue(installCppOption);
                    selection.InstallPython = parseResult.GetValue(installPythonOption);
                    selection.InstallLabView = parseResult.GetValue(installLabViewOption);
                    selection.InstallWPILibTools = parseResult.GetValue(installWpilibToolsOption);
                    selection.InstallElastic = parseResult.GetValue(installElasticOption);
                    selection.InstallAdvantageScope = parseResult.GetValue(installAdvantageScopeOption);
                }

                await using var installer = new CliInstaller();
                return await installer.RunInstallAsync(
                    allUsers,
                    resourcesFileArgument: resourcesFile,
                    artifactsFileArgument: artifactsFile,
                    vsCodeArchiveArgument: vsCodeArchive,
                    force: force,
                    selection: selection);
            });

            var parseResult = rootCommand.Parse(args);
            return await parseResult.InvokeAsync();
        }
    }
}
