using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.Interfaces
{
    public interface IConfigurationProvider
    {
        VsCodeModel VsCodeModel { get; }

        IArchiveExtractor ZipArchive { get; }

        string? ArtifactsFile { get; }

        UpgradeConfig UpgradeConfig { get; }
        FullConfig FullConfig { get; }

        JdkConfig JdkConfig { get; }

        AdvantageScopeConfig AdvantageScopeConfig { get; }

        ElasticConfig ElasticConfig { get; }

        VsCodeConfig VsCodeConfig { get; }

        string InstallDirectory { get; }
    }
}
