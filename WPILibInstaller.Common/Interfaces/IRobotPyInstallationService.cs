using WPILibInstaller.Models;

namespace WPILibInstaller.Interfaces
{
    public interface IRobotPyInstallationService
    {
        Task InstallPython(CancellationToken token, IProgress<InstallProgress>? progress = null);

        Task InstallRobotPy(CancellationToken token, IProgress<InstallProgress>? progress = null);
    }
}
