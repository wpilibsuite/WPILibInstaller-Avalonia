using WPILibInstaller.Models;

namespace WPILibInstaller.Interfaces
{
    public interface IRobotPyInstallationService
    {
        Task InstallPython(IProgress<InstallProgress>? progress = null);

        Task InstallRobotPy(IProgress<InstallProgress>? progress = null);
    }
}
