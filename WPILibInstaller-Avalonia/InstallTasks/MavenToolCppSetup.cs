using System.Threading;
using System.Threading.Tasks;
using System.IO;

using WPILibInstaller.Interfaces;

namespace WPILibInstaller.InstallTasks
{
    public class CppSetupTask : InstallTask
    {

        public CppSetupTask (IConfigurationProvider pConfigurationProvider)
            :base(pConfigurationProvider)
        {
        }

        public override async Task Execute(CancellationToken token)
        {
            Text = "Configuring C++";
            Progress = 50;

            await Task.Yield();
        }
    }

    public class ToolSetupTask : InstallTask
    {
        public ToolSetupTask(
            IConfigurationProvider pConfigurationProvider
        )
        :base(pConfigurationProvider)
        {
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

    public class MavenMetaDataFixerTask : InstallTask
    {

        public MavenMetaDataFixerTask(
            IConfigurationProvider pConfigurationProvider
        )
            :base(pConfigurationProvider)
        {
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

