using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.Interfaces
{
    public interface IConfigurationProvider
    {
        VsCodeModel VsCodeModel { get; }

        IArchiveExtractor ZipArchive { get; }

        UpgradeConfig UpgradeConfig { get; }
        FullConfig FullConfig { get; }

        JdkConfig JdkConfig { get; }

        VsCodeConfig VsCodeConfig { get; }

        string InstallDirectory { get; }
    }
}
