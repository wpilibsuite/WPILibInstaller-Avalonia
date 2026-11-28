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

            bool anyLanguage = selection.AnyLanguage;
            long total = anyLanguage
                ? sizes[SizeCategory.Common]
                : selection.AnyToolsSelected ? sizes[SizeCategory.StandaloneCommon] : 0;

            if (anyLanguage)
            {
                // Java/C++ installs extract the whole artifact archive, so every shared
                // bundle (JDK, tools, dashboards, docs, Maven) lands in the install root.
                total += sizes.Total - sizes[SizeCategory.Common];

                // RunGradleSetup copies the Gradle ZIP from installUtils into each configured
                // Gradle user-home location. The source ZIP is already counted in Common.
                total += sizes[SizeCategory.GradleZip] * GetGradleCopyCount();
            }
            else if (selection.AnyToolsSelected)
            {
                // Standalone tool installs extract only the shared shortcut/config files and
                // ToolsUpdater; the Gradle ZIP and documentation are not extracted.
                total += sizes[SizeCategory.ToolSupport];

                if (selection.InstallWPILibTools)
                {
                    total += sizes[SizeCategory.Jdk] + sizes[SizeCategory.WpilibTools];
                }

                if (selection.InstallElastic)
                {
                    total += sizes[SizeCategory.Elastic];
                }

                if (selection.InstallAdvantageScope)
                {
                    total += sizes[SizeCategory.AdvantageScope];
                }
            }

            if (selection.InstallWPILibTools)
            {
                // ToolsUpdater leaves the source archives in place and writes an additional
                // copy or extracted tree into tools/.
                total += sizes[SizeCategory.JavaToolInstalled] + sizes[SizeCategory.CppToolInstalled];
            }

            if (selection.InstallElastic)
            {
                total += sizes[SizeCategory.ElasticInstalled];
            }

            if (selection.InstallAdvantageScope)
            {
                total += sizes[SizeCategory.AdvantageScopeInstalled];
            }

            if (selection.UsesVsCode)
            {
                // VS Code is downloaded separately, and VS Code extracts each VSIX into its
                // portable data directory in addition to leaving the source VSIX in place.
                total += GetVsCodeInstalledSize();
                total += sizes[SizeCategory.WpilibExtensionInstalled];

                if (anyLanguage)
                {
                    total += sizes[SizeCategory.JavaExtensionInstalled];
                }

                if (selection.InstallCpp)
                {
                    total += sizes[SizeCategory.CppExtensionInstalled];
                }

                if (selection.InstallPython)
                {
                    total += sizes[SizeCategory.PythonExtensionInstalled];
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

                    var entryName = ArchivePathUtils.NormalizeEntryName(extractor.EntryKey);
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
                    sizes.Add(SizeCategory.CppToolInstalled, extractedSize);
                }
                else
                {
                    // Java tools are copied out of tools/artifacts as-is.
                    sizes.Add(SizeCategory.JavaToolInstalled, extractor.EntrySize);
                }
            }
            else if (sizes.IsElasticArchive(entryName) && entryName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                long extractedSize = await GetTarGzExtractedSizeAsync(extractor.OpenEntryStream()).ConfigureAwait(false);
                sizes.Add(SizeCategory.ElasticInstalled, extractedSize);
            }
            else if (sizes.IsAdvantageScopeArchive(entryName) && entryName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                long extractedSize = await GetTarGzExtractedSizeAsync(extractor.OpenEntryStream()).ConfigureAwait(false);
                sizes.Add(SizeCategory.AdvantageScopeInstalled, extractedSize);
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

                    var entryName = ArchivePathUtils.NormalizeEntryName(entry.FullName);
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
            var (downloadedSize, extractedSize) = configurationProvider.VsCodeConfig.GetSize(PlatformUtils.CurrentPlatform);
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

        private enum ExtensionBucket
        {
            Wpilib,
            Java,
            Cpp,
            Python,
        }

        private enum SizeCategory
        {
            Common,
            StandaloneCommon,
            Jdk,
            WpilibTools,
            ToolSupport,
            Maven,
            CppToolchain,
            JavaDocumentation,
            CppDocumentation,
            PythonDocumentation,
            Elastic,
            AdvantageScope,
            GradleZip,
            JavaToolInstalled,
            CppToolInstalled,
            ElasticInstalled,
            AdvantageScopeInstalled,
            WpilibExtensionArchive,
            JavaExtensionArchive,
            CppExtensionArchive,
            PythonExtensionArchive,
            WpilibExtensionInstalled,
            JavaExtensionInstalled,
            CppExtensionInstalled,
            PythonExtensionInstalled,
        }

        private sealed class ArchiveInstallSizes
        {
            private static readonly SizeCategory[] ArchiveCategories =
            [
                SizeCategory.Common,
                SizeCategory.Jdk,
                SizeCategory.WpilibTools,
                SizeCategory.ToolSupport,
                SizeCategory.Maven,
                SizeCategory.CppToolchain,
                SizeCategory.JavaDocumentation,
                SizeCategory.CppDocumentation,
                SizeCategory.PythonDocumentation,
                SizeCategory.Elastic,
                SizeCategory.AdvantageScope,
                SizeCategory.WpilibExtensionArchive,
                SizeCategory.JavaExtensionArchive,
                SizeCategory.CppExtensionArchive,
                SizeCategory.PythonExtensionArchive,
            ];

            private readonly StandaloneInstallPaths standalonePaths;
            private readonly string mavenPrefix;
            private readonly string cppToolchainPrefix;
            private readonly Dictionary<string, ExtensionBucket> extensionFiles = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<SizeCategory, long> sizes = [];

            public ArchiveInstallSizes(IConfigurationProvider configurationProvider)
            {
                standalonePaths = StandaloneInstallPaths.From(configurationProvider);
                mavenPrefix = ArchivePathUtils.FolderPrefix(configurationProvider.UpgradeConfig?.Maven?.Folder);
                cppToolchainPrefix = ArchivePathUtils.FolderPrefix(configurationProvider.FullConfig?.CppToolchain?.Directory);

                var vsCodeConfig = configurationProvider.VsCodeConfig;
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

            public long this[SizeCategory category] => sizes.TryGetValue(category, out var value) ? value : 0;

            public long Total
            {
                get
                {
                    long total = 0;
                    foreach (var category in ArchiveCategories)
                    {
                        total += this[category];
                    }

                    return total;
                }
            }

            public void Add(string entryName, long size)
            {
                bool isGradleZip = standalonePaths.IsGradleZip(entryName);
                if (isGradleZip)
                {
                    Set(SizeCategory.GradleZip, size);
                }

                if (ArchivePathUtils.Matches(entryName, standalonePaths.JdkPrefix))
                {
                    Add(SizeCategory.Jdk, size);
                }
                else if (ArchivePathUtils.Matches(entryName, standalonePaths.ToolsJsonPath)
                         || ArchivePathUtils.Matches(entryName, standalonePaths.ToolsUpdaterPath))
                {
                    Add(SizeCategory.ToolSupport, size);
                }
                else if (ArchivePathUtils.Matches(entryName, standalonePaths.ToolsPrefix))
                {
                    Add(SizeCategory.WpilibTools, size);
                }
                else if (ArchivePathUtils.Matches(entryName, mavenPrefix))
                {
                    Add(SizeCategory.Maven, size);
                }
                else if (ArchivePathUtils.Matches(entryName, cppToolchainPrefix))
                {
                    Add(SizeCategory.CppToolchain, size);
                }
                else if (ArchivePathUtils.Matches(entryName, standalonePaths.ElasticPrefix))
                {
                    Add(SizeCategory.Elastic, size);
                }
                else if (ArchivePathUtils.Matches(entryName, standalonePaths.AdvantageScopePrefix))
                {
                    Add(SizeCategory.AdvantageScope, size);
                }
                else if (ArchivePathUtils.Matches(entryName, DocumentationPrefix))
                {
                    AddDocumentation(entryName, size);
                }
                else if (ArchivePathUtils.Matches(entryName, VsCodeExtensionsPrefix))
                {
                    AddExtension(entryName, size);
                }
                else
                {
                    Add(SizeCategory.Common, size);

                    // Standalone installs only extract this subset of Common. Everything else
                    // here (docs, Gradle ZIP, miscellaneous launcher files) is written by
                    // full Java/C++ installs.
                    if (standalonePaths.IsStandaloneCommon(entryName))
                    {
                        Add(SizeCategory.StandaloneCommon, size);
                    }
                }
            }

            public bool TryGetExtensionBucket(string fileName, out ExtensionBucket bucket)
            {
                return extensionFiles.TryGetValue(fileName, out bucket);
            }

            public bool IsToolArtifact(string entryName)
            {
                return ArchivePathUtils.Matches(entryName, standalonePaths.ToolsArtifactsPrefix);
            }

            public bool IsElasticArchive(string entryName)
            {
                return ArchivePathUtils.Matches(entryName, standalonePaths.ElasticPrefix);
            }

            public bool IsAdvantageScopeArchive(string entryName)
            {
                return ArchivePathUtils.Matches(entryName, standalonePaths.AdvantageScopePrefix);
            }

            public void AddExtensionInstalled(ExtensionBucket bucket, long size)
            {
                Add(InstalledCategory(bucket), size);
            }

            public void Add(SizeCategory category, long size)
            {
                sizes[category] = this[category] + size;
            }

            private void Set(SizeCategory category, long size)
            {
                sizes[category] = size;
            }

            private static SizeCategory ArchiveCategory(ExtensionBucket bucket)
            {
                return bucket switch
                {
                    ExtensionBucket.Wpilib => SizeCategory.WpilibExtensionArchive,
                    ExtensionBucket.Java => SizeCategory.JavaExtensionArchive,
                    ExtensionBucket.Cpp => SizeCategory.CppExtensionArchive,
                    ExtensionBucket.Python => SizeCategory.PythonExtensionArchive,
                    _ => SizeCategory.JavaExtensionArchive,
                };
            }

            private static SizeCategory InstalledCategory(ExtensionBucket bucket)
            {
                return bucket switch
                {
                    ExtensionBucket.Wpilib => SizeCategory.WpilibExtensionInstalled,
                    ExtensionBucket.Java => SizeCategory.JavaExtensionInstalled,
                    ExtensionBucket.Cpp => SizeCategory.CppExtensionInstalled,
                    ExtensionBucket.Python => SizeCategory.PythonExtensionInstalled,
                    _ => SizeCategory.JavaExtensionInstalled,
                };
            }

            private void AddDocumentation(string entryName, long size)
            {
                if (ArchivePathUtils.Matches(entryName, JavaDocumentationPrefix))
                {
                    Add(SizeCategory.JavaDocumentation, size);
                }
                else if (ArchivePathUtils.Matches(entryName, CppDocumentationPrefix))
                {
                    Add(SizeCategory.CppDocumentation, size);
                }
                else if (ArchivePathUtils.Matches(entryName, PythonDocumentationPrefix))
                {
                    Add(SizeCategory.PythonDocumentation, size);
                }
                else
                {
                    Add(SizeCategory.Common, size);
                }
            }

            private void AddExtension(string entryName, long size)
            {
                var fileName = Path.GetFileName(entryName);
                var bucket = extensionFiles.TryGetValue(fileName, out var knownBucket)
                    ? knownBucket
                    : ExtensionBucket.Java;

                // Old installers shipped a combined extension list; count anything unknown
                // as a Java extension so the estimate still includes it.
                Add(ArchiveCategory(bucket), size);
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
        }
    }
}
