using System.Formats.Tar;
using System.IO.Compression;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.Services
{
    /// <summary>
    /// Estimates the installed footprint from the artifact archive plus the second-stage
    /// expansions performed by the installer: VS Code, VS Code extensions, tool archives,
    /// dashboard archives on macOS, and the extra Gradle ZIP copies in the user profile.
    /// </summary>
    public sealed class InstallSizeCalculator : IInstallSizeCalculator
    {
        private const string DocumentationPrefix = "documentation/";
        private const string JavaDocumentationPrefix = DocumentationPrefix + "java/";
        private const string CppDocumentationPrefix = DocumentationPrefix + "cpp/";
        private const string PythonDocumentationPrefix = DocumentationPrefix + "python/";
        private const string VsCodeExtensionsPrefix = "vsCodeExtensions/";
        private const string InstallUtilsPrefix = "installUtils/";
        private const string IconsPrefix = "icons/";

        private readonly IConfigurationProvider configurationProvider;
        private Task<ArchiveInstallSizes?>? sizesTask;

        public InstallSizeCalculator(IConfigurationProvider configurationProvider)
        {
            this.configurationProvider = configurationProvider;
        }

        public async Task<long?> CalculateInstallSizeAsync(InstallSelectionModel selection, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            var sizes = await GetArchiveSizesAsync().ConfigureAwait(false);
            if (sizes == null)
            {
                return null;
            }

            if (!selection.HasAnySelection)
            {
                return 0;
            }

            bool anyLanguage = selection.InstallJava || selection.InstallCpp;
            long total = anyLanguage
                ? sizes.Common
                : selection.AnyToolsSelected ? sizes.StandaloneCommon : 0;

            if (anyLanguage)
            {
                // Java/C++ installs extract the whole artifact archive, so every shared
                // bundle (JDK, tools, dashboards, docs, Maven) lands in the install root.
                total += sizes.Total - sizes.Common;

                // RunGradleSetup copies the Gradle ZIP from installUtils into each configured
                // Gradle user-home location. The source ZIP is already counted in Common.
                total += sizes.GradleZip * GetGradleCopyCount();
            }
            else if (selection.AnyToolsSelected)
            {
                // Standalone tool installs extract only the shared shortcut/config files and
                // ToolsUpdater; the Gradle ZIP and documentation are not extracted.
                total += sizes.ToolSupport;

                if (selection.InstallWPILibTools)
                {
                    total += sizes.Jdk + sizes.WpilibTools;
                }

                if (selection.InstallElastic)
                {
                    total += sizes.Elastic;
                }

                if (selection.InstallAdvantageScope)
                {
                    total += sizes.AdvantageScope;
                }
            }

            if (selection.InstallWPILibTools)
            {
                // ToolsUpdater leaves the source archives in place and writes an additional
                // copy or extracted tree into tools/.
                total += sizes.JavaToolInstalled + sizes.CppToolInstalled;
            }

            if (selection.InstallElastic)
            {
                total += sizes.ElasticInstalled;
            }

            if (selection.InstallAdvantageScope)
            {
                total += sizes.AdvantageScopeInstalled;
            }

            if (selection.UsesVsCode)
            {
                // VS Code is downloaded separately, and VS Code extracts each VSIX into its
                // portable data directory in addition to leaving the source VSIX in place.
                total += GetVsCodeInstalledSize();
                total += sizes.WpilibExtensionInstalled;

                if (selection.InstallJava || selection.InstallCpp)
                {
                    total += sizes.JavaExtensionsInstalled;
                }

                if (selection.InstallCpp)
                {
                    total += sizes.CppExtensionsInstalled;
                }

                if (selection.InstallPython)
                {
                    total += sizes.PythonExtensionsInstalled;
                }
            }

            return total;
        }

        public long? GetAvailableSpaceBytes()
        {
            var installDirectory = configurationProvider.InstallDirectory;
            if (string.IsNullOrWhiteSpace(installDirectory))
            {
                return null;
            }

            try
            {
                var root = Path.GetPathRoot(Path.GetFullPath(installDirectory));
                if (string.IsNullOrWhiteSpace(root))
                {
                    return null;
                }

                return new DriveInfo(root).AvailableFreeSpace;
            }
            catch (Exception ex) when (ex is ArgumentException
                                           or NotSupportedException
                                           or IOException
                                           or UnauthorizedAccessException
                                           or InvalidOperationException
                                           or System.Security.SecurityException)
            {
                return null;
            }
        }

        private int GetGradleCopyCount()
        {
            return configurationProvider.FullConfig?.Gradle?.ExtractLocations?.Length ?? 0;
        }

        private Task<ArchiveInstallSizes?> GetArchiveSizesAsync()
        {
            return sizesTask ??= Task.Run(ReadArchiveSizesAsync);
        }

        private async Task<ArchiveInstallSizes?> ReadArchiveSizesAsync()
        {
            var path = configurationProvider.ArtifactsFile;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                var sizes = new ArchiveInstallSizes(configurationProvider);
                await using var stream = File.OpenRead(path);
                using var extractor = ArchiveUtils.OpenArchive(stream);

                while (await extractor.MoveToNextEntryAsync().ConfigureAwait(false))
                {
                    if (extractor.EntryIsDirectory)
                    {
                        continue;
                    }

                    var entryName = NormalizeEntryName(extractor.EntryKey);
                    sizes.Add(entryName, extractor.EntrySize);

                    try
                    {
                        await AddNestedSizesAsync(extractor, entryName, sizes).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is IOException
                                                  or UnauthorizedAccessException
                                                  or InvalidDataException
                                                  or NotSupportedException
                                                  or ArgumentException
                                                  or ObjectDisposedException
                                                  or InvalidOperationException)
                    {
                        // A nested archive is optional information for the estimate.
                    }
                }

                return sizes;
            }
            catch (Exception ex) when (ex is IOException
                                           or UnauthorizedAccessException
                                           or InvalidDataException
                                           or NotSupportedException
                                           or ArgumentException
                                           or ObjectDisposedException)
            {
                // A size estimate is optional; an unreadable archive simply means "unknown".
                return null;
            }
        }

        private static async Task AddNestedSizesAsync(
            IArchiveExtractor extractor,
            string entryName,
            ArchiveInstallSizes sizes)
        {
            if (entryName.StartsWith(VsCodeExtensionsPrefix, StringComparison.OrdinalIgnoreCase)
                && entryName.EndsWith(".vsix", StringComparison.OrdinalIgnoreCase))
            {
                var bucket = sizes.TryGetExtensionBucket(Path.GetFileName(entryName), out var extensionBucket)
                    ? extensionBucket
                    : ExtensionBucket.Java;
                long installedSize = await GetZipExtractedSizeAsync(extractor.OpenEntryStream()).ConfigureAwait(false);
                sizes.AddExtensionInstalled(bucket, installedSize);
            }
            else if (sizes.IsToolArtifact(entryName))
            {
                if (entryName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    long extractedSize = await GetZipExtractedSizeAsync(
                        extractor.OpenEntryStream(),
                        ToolPlatformPrefix()).ConfigureAwait(false);
                    sizes.AddCppToolInstalled(extractedSize);
                }
                else
                {
                    // Java tools are copied out of tools/artifacts as-is.
                    sizes.AddJavaToolInstalled(extractor.EntrySize);
                }
            }
            else if (sizes.IsElasticArchive(entryName) && entryName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                long extractedSize = await GetTarGzExtractedSizeAsync(extractor.OpenEntryStream()).ConfigureAwait(false);
                sizes.AddElasticInstalled(extractedSize);
            }
            else if (sizes.IsAdvantageScopeArchive(entryName) && entryName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                long extractedSize = await GetTarGzExtractedSizeAsync(extractor.OpenEntryStream()).ConfigureAwait(false);
                sizes.AddAdvantageScopeInstalled(extractedSize);
            }
        }

        private static async Task<long> GetZipExtractedSizeAsync(Stream source, string? entryPrefix = null)
        {
            await using (source)
            {
                using var buffer = new MemoryStream();
                await source.CopyToAsync(buffer).ConfigureAwait(false);
                buffer.Position = 0;

                using var archive = new ZipArchive(buffer, ZipArchiveMode.Read);
                long total = 0;
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        continue;
                    }

                    var entryName = NormalizeEntryName(entry.FullName);
                    if (entryPrefix == null
                        || entryName.StartsWith(entryPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        total += entry.Length;
                    }
                }

                return total;
            }
        }

        private static async Task<long> GetTarGzExtractedSizeAsync(Stream source)
        {
            await using (source)
            {
                using var gzip = new GZipStream(source, CompressionMode.Decompress);
                using var reader = new TarReader(gzip, leaveOpen: false);

                long total = 0;
                while (await reader.GetNextEntryAsync().ConfigureAwait(false) is { } entry)
                {
                    if (entry.EntryType != TarEntryType.Directory)
                    {
                        total += entry.Length;
                    }
                }

                return total;
            }
        }

        private long GetVsCodeInstalledSize()
        {
            var config = configurationProvider.VsCodeConfig;
            long downloadedSize = 0;
            long extractedSize = 0;

            switch (PlatformUtils.CurrentPlatform)
            {
                case Platform.Win64:
                    downloadedSize = config.VsCodeWindowsSize;
                    extractedSize = config.VsCodeWindowsExtractedSize;
                    break;
                case Platform.WinArm64:
                    downloadedSize = config.VsCodeWindowsArm64Size;
                    extractedSize = config.VsCodeWindowsArm64ExtractedSize;
                    break;
                case Platform.Linux64:
                    downloadedSize = config.VsCodeLinuxSize;
                    extractedSize = config.VsCodeLinuxExtractedSize;
                    break;
                case Platform.LinuxArm64:
                    downloadedSize = config.VsCodeLinuxArm64Size;
                    extractedSize = config.VsCodeLinuxArm64ExtractedSize;
                    break;
                case Platform.Mac64:
                case Platform.MacArm64:
                    downloadedSize = config.VsCodeMacSize;
                    extractedSize = config.VsCodeMacExtractedSize;
                    break;
            }

            if (extractedSize > 0)
            {
                return extractedSize;
            }

            // Older resource files only publish the compressed download size.
            return (long)Math.Round(downloadedSize * GetVsCodeExpansionFactor());
        }

        private static double GetVsCodeExpansionFactor()
        {
            return PlatformUtils.CurrentPlatform switch
            {
                Platform.Win64 or Platform.WinArm64 => 2.95,
                Platform.Mac64 or Platform.MacArm64 => 2.65,
                Platform.Linux64 or Platform.LinuxArm64 => 2.9,
                _ => 2.75,
            };
        }

        private static string ToolPlatformPrefix()
        {
            return PlatformUtils.CurrentPlatform switch
            {
                Platform.Win64 => "windows/x86-64/",
                Platform.WinArm64 => "windows/arm64/",
                Platform.Linux64 => "linux/x86-64/",
                Platform.LinuxArm64 => "linux/arm64/",
                Platform.Mac64 or Platform.MacArm64 => "osx/universal/",
                _ => "!/",
            };
        }

        private static string NormalizeEntryName(string entryName)
        {
            return entryName.Replace('\\', '/').TrimStart('.', '/');
        }

        private enum ExtensionBucket
        {
            Wpilib,
            Java,
            Cpp,
            Python,
        }

        private sealed class ArchiveInstallSizes
        {
            private readonly string jdkPrefix;
            private readonly string toolsPrefix;
            private readonly string toolsJsonPath;
            private readonly string toolsUpdaterPath;
            private readonly string toolsArtifactsPrefix;
            private readonly string mavenPrefix;
            private readonly string cppToolchainPrefix;
            private readonly string elasticPrefix;
            private readonly string advantageScopePrefix;
            private readonly string? gradleZipFileName;
            private readonly Dictionary<string, ExtensionBucket> extensionFiles = new(StringComparer.OrdinalIgnoreCase);

            public ArchiveInstallSizes(IConfigurationProvider configurationProvider)
            {
                jdkPrefix = FolderPrefix(configurationProvider.JdkConfig?.Folder);
                toolsPrefix = FolderPrefix(configurationProvider.UpgradeConfig?.Tools?.Folder);
                mavenPrefix = FolderPrefix(configurationProvider.UpgradeConfig?.Maven?.Folder);
                cppToolchainPrefix = FolderPrefix(configurationProvider.FullConfig?.CppToolchain?.Directory);
                elasticPrefix = FolderPrefix(configurationProvider.ElasticConfig?.Folder);
                advantageScopePrefix = FolderPrefix(configurationProvider.AdvantageScopeConfig?.Folder);

                var toolsConfig = configurationProvider.UpgradeConfig?.Tools;
                var toolsFolder = toolsConfig?.Folder;
                toolsJsonPath = toolsPrefix + "tools.json";
                toolsUpdaterPath = string.IsNullOrWhiteSpace(toolsConfig?.UpdaterExe)
                    ? "!/"
                    : toolsPrefix + toolsConfig.UpdaterExe;
                toolsArtifactsPrefix = string.IsNullOrWhiteSpace(toolsFolder)
                    ? "!/"
                    : FolderPrefix(toolsFolder + "/artifacts");

                gradleZipFileName = configurationProvider.FullConfig?.Gradle?.ZipName;

                var vsCodeConfig = configurationProvider.VsCodeConfig;
                if (vsCodeConfig != null)
                {
                    RegisterExtension(vsCodeConfig.WPILibExtension, ExtensionBucket.Wpilib);
                    RegisterExtensions(vsCodeConfig.JavaExtensions, ExtensionBucket.Java);
                    RegisterExtensions(vsCodeConfig.CppExtensions, ExtensionBucket.Cpp);
                    RegisterExtensions(vsCodeConfig.PythonExtensions, ExtensionBucket.Python);
                    if (vsCodeConfig.JavaExtensions is not { Length: > 0 })
                    {
                        // Older config files only contain the combined extension list.
                        RegisterExtensions(vsCodeConfig.ThirdPartyExtensions, ExtensionBucket.Java);
                    }
                }
            }

            public long Common { get; private set; }

            public long StandaloneCommon { get; private set; }

            public long Jdk { get; private set; }

            public long WpilibTools { get; private set; }

            public long ToolSupport { get; private set; }

            public long Maven { get; private set; }

            public long CppToolchain { get; private set; }

            public long JavaDocumentation { get; private set; }

            public long CppDocumentation { get; private set; }

            public long PythonDocumentation { get; private set; }

            public long Elastic { get; private set; }

            public long AdvantageScope { get; private set; }

            public long GradleZip { get; private set; }

            public long JavaToolInstalled { get; private set; }

            public long CppToolInstalled { get; private set; }

            public long ElasticInstalled { get; private set; }

            public long AdvantageScopeInstalled { get; private set; }

            public long WpilibExtension { get; private set; }

            public long JavaExtensions { get; private set; }

            public long CppExtensions { get; private set; }

            public long PythonExtensions { get; private set; }

            public long WpilibExtensionInstalled { get; private set; }

            public long JavaExtensionsInstalled { get; private set; }

            public long CppExtensionsInstalled { get; private set; }

            public long PythonExtensionsInstalled { get; private set; }

            public long Total => Common
                                 + Jdk
                                 + WpilibTools
                                 + ToolSupport
                                 + Maven
                                 + CppToolchain
                                 + JavaDocumentation
                                 + CppDocumentation
                                 + PythonDocumentation
                                 + Elastic
                                 + AdvantageScope
                                 + WpilibExtension
                                 + JavaExtensions
                                 + CppExtensions
                                 + PythonExtensions;

            public void Add(string entryName, long size)
            {
                bool isGradleZip = string.Equals(Path.GetFileName(entryName), gradleZipFileName, StringComparison.OrdinalIgnoreCase)
                                   && Matches(entryName, InstallUtilsPrefix);
                if (isGradleZip)
                {
                    GradleZip = size;
                }

                if (Matches(entryName, jdkPrefix))
                {
                    Jdk += size;
                }
                else if (Matches(entryName, toolsJsonPath) || Matches(entryName, toolsUpdaterPath))
                {
                    ToolSupport += size;
                }
                else if (Matches(entryName, toolsPrefix))
                {
                    WpilibTools += size;
                }
                else if (Matches(entryName, mavenPrefix))
                {
                    Maven += size;
                }
                else if (Matches(entryName, cppToolchainPrefix))
                {
                    CppToolchain += size;
                }
                else if (Matches(entryName, elasticPrefix))
                {
                    Elastic += size;
                }
                else if (Matches(entryName, advantageScopePrefix))
                {
                    AdvantageScope += size;
                }
                else if (Matches(entryName, DocumentationPrefix))
                {
                    AddDocumentation(entryName, size);
                }
                else if (Matches(entryName, VsCodeExtensionsPrefix))
                {
                    AddExtension(entryName, size);
                }
                else
                {
                    Common += size;

                    // These are the only unclassified files extracted by standalone tool
                    // installs. Everything else in Common (docs, Gradle ZIP, miscellaneous
                    // launcher files) is only written by full Java/C++ installs.
                    if (Matches(entryName, IconsPrefix)
                        || (Matches(entryName, InstallUtilsPrefix) && !isGradleZip))
                    {
                        StandaloneCommon += size;
                    }
                }
            }

            public bool TryGetExtensionBucket(string fileName, out ExtensionBucket bucket)
            {
                return extensionFiles.TryGetValue(fileName, out bucket);
            }

            public bool IsToolArtifact(string entryName)
            {
                return Matches(entryName, toolsArtifactsPrefix);
            }

            public bool IsElasticArchive(string entryName)
            {
                return Matches(entryName, elasticPrefix);
            }

            public bool IsAdvantageScopeArchive(string entryName)
            {
                return Matches(entryName, advantageScopePrefix);
            }

            public void AddExtensionInstalled(ExtensionBucket bucket, long size)
            {
                switch (bucket)
                {
                    case ExtensionBucket.Wpilib:
                        WpilibExtensionInstalled += size;
                        break;
                    case ExtensionBucket.Java:
                        JavaExtensionsInstalled += size;
                        break;
                    case ExtensionBucket.Cpp:
                        CppExtensionsInstalled += size;
                        break;
                    case ExtensionBucket.Python:
                        PythonExtensionsInstalled += size;
                        break;
                }
            }

            public void AddJavaToolInstalled(long size)
            {
                JavaToolInstalled += size;
            }

            public void AddCppToolInstalled(long size)
            {
                CppToolInstalled += size;
            }

            public void AddElasticInstalled(long size)
            {
                ElasticInstalled += size;
            }

            public void AddAdvantageScopeInstalled(long size)
            {
                AdvantageScopeInstalled += size;
            }

            private void AddDocumentation(string entryName, long size)
            {
                if (Matches(entryName, JavaDocumentationPrefix))
                {
                    JavaDocumentation += size;
                }
                else if (Matches(entryName, CppDocumentationPrefix))
                {
                    CppDocumentation += size;
                }
                else if (Matches(entryName, PythonDocumentationPrefix))
                {
                    PythonDocumentation += size;
                }
                else
                {
                    Common += size;
                }
            }

            private void AddExtension(string entryName, long size)
            {
                var fileName = Path.GetFileName(entryName);
                if (extensionFiles.TryGetValue(fileName, out var bucket))
                {
                    AddExtensionArchive(bucket, size);
                    return;
                }

                // Old installers shipped a combined extension list; count anything unknown
                // as a Java extension so the estimate still includes it.
                AddExtensionArchive(ExtensionBucket.Java, size);
            }

            private void AddExtensionArchive(ExtensionBucket bucket, long size)
            {
                switch (bucket)
                {
                    case ExtensionBucket.Wpilib:
                        WpilibExtension += size;
                        break;
                    case ExtensionBucket.Java:
                        JavaExtensions += size;
                        break;
                    case ExtensionBucket.Cpp:
                        CppExtensions += size;
                        break;
                    case ExtensionBucket.Python:
                        PythonExtensions += size;
                        break;
                }
            }

            private void RegisterExtensions(Extension[]? extensions, ExtensionBucket bucket)
            {
                foreach (var extension in extensions ?? [])
                {
                    RegisterExtension(extension, bucket);
                }
            }

            private void RegisterExtension(Extension? extension, ExtensionBucket bucket)
            {
                if (extension == null || string.IsNullOrWhiteSpace(extension.Vsix))
                {
                    return;
                }

                extensionFiles[Path.GetFileName(extension.Vsix)] = bucket;
            }

            private static string FolderPrefix(string? folder)
            {
                if (string.IsNullOrWhiteSpace(folder))
                {
                    // A prefix that cannot match any real archive entry.
                    return "!/";
                }

                return NormalizeEntryName(folder).TrimEnd('/') + "/";
            }

            private static bool Matches(string entryName, string prefix)
            {
                return prefix.Length > 0 && entryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
