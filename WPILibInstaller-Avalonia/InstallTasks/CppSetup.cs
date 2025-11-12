using System.Threading;
using System.Threading.Tasks;
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
}

