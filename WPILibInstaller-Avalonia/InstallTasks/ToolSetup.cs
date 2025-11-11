using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPILibInstaller.Interfaces;

namespace WPILibInstaller.InstallTasks
{
    public class ToolSetupTask : InstallTask
    {

        private readonly IConfigurationProvider configurationProvider;

        public ToolSetupTask(
            IConfigurationProvider pConfigurationProvider
        )
        {
            configurationProvider = pConfigurationProvider;
        }

        public override async Task Execute(CancellationToken token)
        {
            Text = "Configuring Tools";
            Progress = 50;

            await Utilities.RunJavaJar(configurationProvider.InstallDirectory,
                Path.Combine(configurationProvider.InstallDirectory,
                configurationProvider.UpgradeConfig.Tools.Folder,
                configurationProvider.UpgradeConfig.Tools.UpdaterJar), 30000);
        }
    }
}
