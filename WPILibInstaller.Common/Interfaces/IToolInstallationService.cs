using WPILibInstaller.Models;

namespace WPILibInstaller.Interfaces
{
    public interface IToolInstallationService
    {
        Task StopGradleDaemons(IProgress<InstallProgress>? progress = null, CancellationToken token = default);

        Task RunGradleSetup(IProgress<InstallProgress>? progress = null);

        Task RunToolSetup(bool wpilibTools, bool elastic, bool advantageScope, IProgress<InstallProgress>? progress = null);

        Task RunCppSetup(IProgress<InstallProgress>? progress = null);

        Task RunMavenMetaDataFixer(IProgress<InstallProgress>? progress = null);
    }
}
