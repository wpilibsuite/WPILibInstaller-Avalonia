using WPILibInstaller.Interfaces;

namespace WPILibInstaller.Utils
{
    /// <summary>
    /// The archive paths used by tool-only installs. Both the extractor and the size
    /// estimator use this so they cannot drift apart.
    /// </summary>
    internal sealed class StandaloneInstallPaths
    {
        private const string InstallUtilsFolder = "installUtils";
        private const string IconsFilterPrefix = "icons";
        private const string IconsSizePrefix = "icons/";

        private readonly string[] commonFiles;

        private StandaloneInstallPaths(
            string[] commonFiles,
            string jdkPrefix,
            string toolsPrefix,
            string toolsJsonPath,
            string toolsUpdaterPath,
            string toolsArtifactsPrefix,
            string elasticPrefix,
            string advantageScopePrefix,
            string? gradleZipFileName)
        {
            this.commonFiles = commonFiles;
            JdkPrefix = jdkPrefix;
            ToolsPrefix = toolsPrefix;
            ToolsJsonPath = toolsJsonPath;
            ToolsUpdaterPath = toolsUpdaterPath;
            ToolsArtifactsPrefix = toolsArtifactsPrefix;
            ElasticPrefix = elasticPrefix;
            AdvantageScopePrefix = advantageScopePrefix;
            GradleZipFileName = gradleZipFileName;
        }

        public string JdkPrefix { get; }

        public string ToolsPrefix { get; }

        public string ToolsJsonPath { get; }

        public string ToolsUpdaterPath { get; }

        public string ToolsArtifactsPrefix { get; }

        public string ElasticPrefix { get; }

        public string AdvantageScopePrefix { get; }

        public string? GradleZipFileName { get; }

        public static StandaloneInstallPaths From(IConfigurationProvider configurationProvider)
        {
            var tools = configurationProvider.UpgradeConfig.Tools;
            var toolsFolder = tools.Folder;

            return new StandaloneInstallPaths(
                [
                    ArchivePathUtils.FilePath(InstallUtilsFolder, "upgradeConfig.json"),
                    ArchivePathUtils.FilePath(InstallUtilsFolder, "fullConfig.json"),
                    ArchivePathUtils.FilePath(InstallUtilsFolder, "jdkConfig.json"),
                    ArchivePathUtils.FilePath(InstallUtilsFolder, "vscodeConfig.json"),
                    ArchivePathUtils.FilePath(InstallUtilsFolder, "WPILibShortcutCreator.exe"),
                ],
                ArchivePathUtils.FolderPrefix(configurationProvider.JdkConfig.Folder),
                ArchivePathUtils.FolderPrefix(toolsFolder),
                ArchivePathUtils.FilePath(toolsFolder, "tools.json"),
                ArchivePathUtils.FilePath(toolsFolder, tools.UpdaterExe),
                ArchivePathUtils.FolderPrefix(toolsFolder + "/artifacts"),
                ArchivePathUtils.FolderPrefix(configurationProvider.ElasticConfig.Folder),
                ArchivePathUtils.FolderPrefix(configurationProvider.AdvantageScopeConfig.Folder),
                configurationProvider.FullConfig?.Gradle?.ZipName);
        }

        public string[] GetExtractionFilter(bool wpilibTools, bool elastic, bool advantageScope)
        {
            List<string> filter = [.. commonFiles, IconsFilterPrefix, ToolsJsonPath, ToolsUpdaterPath];

            if (wpilibTools)
            {
                // The WPILib tools are Java applications and live under the tools folder.
                filter.Add(JdkPrefix);
                filter.Add(ToolsPrefix);
            }

            if (elastic)
            {
                filter.Add(ElasticPrefix);
            }

            if (advantageScope)
            {
                filter.Add(AdvantageScopePrefix);
            }

            return [.. filter];
        }

        public bool IsStandaloneCommon(string normalizedEntryName)
        {
            return ArchivePathUtils.Matches(normalizedEntryName, IconsSizePrefix)
                   || (ArchivePathUtils.Matches(normalizedEntryName, ArchivePathUtils.FolderPrefix(InstallUtilsFolder))
                       && !IsGradleZip(normalizedEntryName));
        }

        public bool IsGradleZip(string normalizedEntryName)
        {
            return ArchivePathUtils.Matches(normalizedEntryName, ArchivePathUtils.FolderPrefix(InstallUtilsFolder))
                   && string.Equals(
                       Path.GetFileName(normalizedEntryName),
                       GradleZipFileName,
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
