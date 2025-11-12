using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using DynamicData;

using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.InstallTasks
{
    public class VsCodeExtensionsSetupTask : InstallTask
    {

        private readonly IVsCodeInstallLocationProvider vsInstallProvider;

        public VsCodeExtensionsSetupTask(
            IVsCodeInstallLocationProvider pVsInstallProvider,
            IConfigurationProvider pConfigurationProvider
        )
            : base(pConfigurationProvider)
        {
            vsInstallProvider = pVsInstallProvider;
        }

        public override async Task Execute(CancellationToken token)
        {
            if (!vsInstallProvider.Model.InstallExtensions) return;

            string codeExe;

            var currentPlatform = PlatformUtils.CurrentPlatform;
            switch (currentPlatform)
            {
                case Platform.Win64:
                    codeExe = Path.Combine(configurationProvider.InstallDirectory, "vscode", "bin", "code.cmd");
                    break;
                case Platform.MacArm64:
                case Platform.Mac64:
                    var appDirectories = Directory.GetDirectories(Path.Combine(configurationProvider.InstallDirectory, "vscode"), "*.app");
                    if (appDirectories.Length != 1)
                    {
                        throw new InvalidOperationException("Expected exactly one .app directory in the vscode folder.");
                    }
                    codeExe = Path.Combine(appDirectories[0], "Contents", "Resources", "app", "bin", "code");
                    break;
                case Platform.Linux64:
                    codeExe = Path.Combine(configurationProvider.InstallDirectory, "vscode", "VSCode-linux-x64", "bin", "code");
                    break;
                case Platform.LinuxArm64:
                    codeExe = Path.Combine(configurationProvider.InstallDirectory, "vscode", "VSCode-linux-arm64", "bin", "code");
                    break;
                default:
                    throw new PlatformNotSupportedException("Invalid platform");
            }

            // Load existing extensions

            var versions = await Task.Run(() =>
            {
                var startInfo = new ProcessStartInfo(codeExe, "--list-extensions --show-versions")
                {
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                var proc = Process.Start(startInfo);
                proc!.WaitForExit();
                var lines = new List<(string name, WPIVersion version)>();
                while (true)
                {
                    string? line = proc.StandardOutput.ReadLine();
                    if (line == null)
                    {
                        return lines;
                    }

                    if (line.Contains('@'))
                    {
                        var split = line.Split('@');
                        lines.Add((split[0], new WPIVersion(split[1])));
                    }
                }
            });

            var availableToInstall = new List<(Extension extension, WPIVersion version, int sortOrder)>
            {
                (configurationProvider.VsCodeConfig.WPILibExtension,
                new WPIVersion(configurationProvider.VsCodeConfig.WPILibExtension.Version), int.MaxValue)
            };

            for (int i = 0; i < configurationProvider.VsCodeConfig.ThirdPartyExtensions.Length; i++)
            {
                availableToInstall.Add((configurationProvider.VsCodeConfig.ThirdPartyExtensions[i],
                    new WPIVersion(configurationProvider.VsCodeConfig.ThirdPartyExtensions[i].Version), i));
            }

            var maybeUpdates = availableToInstall.Where(x => versions.Select(y => y.name).Contains(x.extension.Name)).ToList();
            var newInstall = availableToInstall.Except(maybeUpdates).ToList();

            var definitelyUpdate = maybeUpdates.Join(versions, x => x.extension.Name, y => y.name,
                (newVersion, existing) => (newVersion, existing))
                .Where(x => x.newVersion.version > x.existing.version).Select(x => x.newVersion);

            var installs = definitelyUpdate.Concat(newInstall)
                                           .OrderBy(x => x.sortOrder)
                                           .Select(x => x.extension)
                                           .ToArray();

            Text = "Installing Extensions";


            int idx = 0;
            double end = installs.Length;
            Progress = 0;
            foreach (var item in installs)
            {
                var startInfo = new ProcessStartInfo(codeExe, "--install-extension " + Path.Combine(configurationProvider.InstallDirectory, "vsCodeExtensions", item.Vsix))
                {
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                await Task.Run(() =>
                {
                    var proc = Process.Start(startInfo);
                    proc!.WaitForExit();
                });

                idx++;

                double percentage = (idx / end) * 100;
                if (percentage > 100) percentage = 100;
                if (percentage < 0) percentage = 0;
                Progress = (int)percentage;
            }
        }
    }
}
