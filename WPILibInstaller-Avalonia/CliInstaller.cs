using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;
using File = System.IO.File;

namespace WPILibInstaller
{
    /// <summary>
    /// CLI entrypoint installer. Implements IConfigurationProvider, IToInstallProvider, IVsCodeInstallLocationProvider
    /// so it can be used with the existing installer logic.
    /// </summary>
    public sealed class CliInstaller : IConfigurationProvider, IToInstallProvider, IVsCodeInstallLocationProvider, IDisposable
    {
        public VsCodeModel VsCodeModel { get; private set; } = null!;
        public IArchiveExtractor ZipArchive { get; private set; } = null!;
        public UpgradeConfig UpgradeConfig { get; private set; } = null!;
        public FullConfig FullConfig { get; private set; } = null!;
        public JdkConfig JdkConfig { get; private set; } = null!;
        public AdvantageScopeConfig AdvantageScopeConfig { get; private set; } = null!;
        public ElasticConfig ElasticConfig { get; private set; } = null!;
        public VsCodeConfig VsCodeConfig { get; private set; } = null!;
        public string InstallDirectory { get; private set; } = "";
        public InstallSelectionModel Model { get; private set; } = new InstallSelectionModel();

        VsCodeModel IVsCodeInstallLocationProvider.Model => VsCodeModel;
        InstallSelectionModel IToInstallProvider.Model => Model;

        private string? _resourcesFile;
        private string? _artifactsFile;

        // Keep the artifacts stream alive for the lifetime of ZipArchive extractor
        private FileStream? _artifactsStream;

        public async Task<int> RunInstallAsync(bool allUsers, string installMode = "all")
        {
            try
            {
                Console.WriteLine("WPILib Installer - CLI Mode");
                Console.WriteLine("============================\n");

                // Validate install mode
                if (installMode != "all" && installMode != "tools")
                {
                    Console.Error.WriteLine($"Error: Invalid install mode '{installMode}'. Valid options are 'all' or 'tools'.");
                    return 1;
                }

                if (!InstallerFileUtils.TryFindInstallerFiles(out _resourcesFile, out _artifactsFile))
                {
                    Console.Error.WriteLine("Error: Could not find installer files in current directory (or macOS volume).");
                    Console.Error.WriteLine("Expected files: *-resources.zip and *-artifacts.tar.gz (or .zip on Windows)");
                    return 1;
                }

                Console.WriteLine($"Found resources: {Path.GetFileName(_resourcesFile)}");
                Console.WriteLine($"Found artifacts: {Path.GetFileName(_artifactsFile)}");

                if (!await LoadConfigurationAsync(_resourcesFile!, _artifactsFile!))
                {
                    Console.Error.WriteLine("Error: Failed to load configuration from resources.");
                    return 1;
                }

                InstallDirectory = InstallerFileUtils.ComputeInstallDirectory(allUsers, UpgradeConfig.FrcYear);
                Console.WriteLine($"Installing to: {InstallDirectory}");
                Console.WriteLine($"Install mode: {installMode}\n");

                // Configure installation mode
                bool installEverything = installMode == "all";
                Model.InstallEverything = installEverything;
                Model.InstallTools = !installEverything;
                Model.InstallAsAdmin = allUsers;

                using var cts = new CancellationTokenSource();

                // Download and prepare VS Code before installation (only for "all" mode)
                if (installEverything)
                {
                    await DownloadAndPrepareVsCodeAsync(cts.Token);
                }

                // Create services (pass null for IProgramWindow since CLI doesn't show dialogs)
                var archiveService = new Services.ArchiveExtractionService(this, null!);
                var vsCodeService = new Services.VsCodeInstallationService(this, this);
                var toolService = new Services.ToolInstallationService(this);
                var shortcutService = new Services.ShortcutService(this, this, this, null!);

                // Run installation steps
                var progress = new Progress<InstallProgress>(p =>
                {
                    if (!string.IsNullOrEmpty(p.StatusText))
                    {
                        Console.WriteLine($"  {p.StatusText} ({p.Percentage}%)");
                    }
                });

                if (installEverything)
                {
                    // Full installation
                    Console.WriteLine("\n[1/9] Extracting archive...");
                    await archiveService.ExtractArchive(cts.Token, null, progress);

                    Console.WriteLine("[2/9] Setting up Gradle...");
                    await toolService.RunGradleSetup(progress);

                    Console.WriteLine("[3/9] Setting up tools...");
                    await toolService.RunToolSetup(progress);

                    Console.WriteLine("[4/9] Setting up C++...");
                    await toolService.RunCppSetup(progress);

                    Console.WriteLine("[5/9] Fixing Maven metadata...");
                    await toolService.RunMavenMetaDataFixer(progress);

                    Console.WriteLine("[6/9] Installing VS Code...");
                    await vsCodeService.RunVsCodeSetup(cts.Token, progress);

                    Console.WriteLine("[7/9] Configuring VS Code settings...");
                    await vsCodeService.ConfigureVsCodeSettings();

                    Console.WriteLine("[8/9] Installing VS Code extensions...");
                    await vsCodeService.RunVsCodeExtensionsSetup(progress);

                    Console.WriteLine("[9/9] Creating shortcuts...");
                    await shortcutService.RunShortcutCreator(cts.Token);
                }
                else
                {
                    // Tools-only installation
                    Console.WriteLine("\n[1/3] Extracting JDK and Tools...");
                    await archiveService.ExtractJDKAndTools(cts.Token, progress);

                    Console.WriteLine("[2/3] Setting up tools...");
                    await toolService.RunToolSetup(progress);

                    Console.WriteLine("[3/3] Creating shortcuts...");
                    await shortcutService.RunShortcutCreator(cts.Token);
                }

                Console.WriteLine("\n✓ Installation completed successfully!");
                return 0;
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("\n✗ Installation cancelled.");
                return 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\n✗ Installation failed: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            try { _artifactsStream?.Dispose(); } catch { /* ignore */ }
            _artifactsStream = null;
        }


        private async Task<bool> LoadConfigurationAsync(string resourcesZipPath, string artifactsPath)
        {
            try
            {
                using var resourcesStream = File.OpenRead(resourcesZipPath);
                using var zipFile = new ZipArchive(resourcesStream, ZipArchiveMode.Read);

                VsCodeConfig = await LoadJsonFromZipAsync<VsCodeConfig>(zipFile, "vscodeConfig.json");
                JdkConfig = await LoadJsonFromZipAsync<JdkConfig>(zipFile, "jdkConfig.json");
                AdvantageScopeConfig = await LoadJsonFromZipAsync<AdvantageScopeConfig>(zipFile, "advantageScopeConfig.json");
                ElasticConfig = await LoadJsonFromZipAsync<ElasticConfig>(zipFile, "elasticConfig.json");
                FullConfig = await LoadJsonFromZipAsync<FullConfig>(zipFile, "fullConfig.json");
                UpgradeConfig = await LoadJsonFromZipAsync<UpgradeConfig>(zipFile, "upgradeConfig.json");

                VsCodeModel = BuildVsCodeModel(VsCodeConfig);

                if (!VerifyPlatform(UpgradeConfig))
                {
                    Console.Error.WriteLine($"Error: This installer is for {UpgradeConfig.InstallerType} but you are running on {GetCurrentPlatformType()}");
                    return false;
                }

                _artifactsStream = File.OpenRead(artifactsPath);
                ZipArchive = ArchiveUtils.OpenArchive(_artifactsStream);

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error loading configuration: {ex.Message}");
                return false;
            }
        }

        private static async Task<T> LoadJsonFromZipAsync<T>(ZipArchive archive, string entryName)
        {
            var entry = archive.GetEntry(entryName) ?? throw new InvalidOperationException($"Missing {entryName}");
            await using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            var json = await reader.ReadToEndAsync();

            return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error
            }) ?? throw new InvalidOperationException($"Invalid {entryName}");
        }

        private static VsCodeModel BuildVsCodeModel(VsCodeConfig cfg)
        {
            var model = new VsCodeModel(cfg.VsCodeVersion);

            model.Platforms[Platform.Win64] = new VsCodeModel.PlatformData(cfg.VsCodeWindowsUrl, cfg.VsCodeWindowsName, cfg.VsCodeWindowsHash);
            model.Platforms[Platform.Linux64] = new VsCodeModel.PlatformData(cfg.VsCodeLinuxUrl, cfg.VsCodeLinuxName, cfg.VsCodeLinuxHash);
            model.Platforms[Platform.LinuxArm64] = new VsCodeModel.PlatformData(cfg.VsCodeLinuxArm64Url, cfg.VsCodeLinuxArm64Name, cfg.VsCodeLinuxArm64Hash);
            model.Platforms[Platform.Mac64] = new VsCodeModel.PlatformData(cfg.VsCodeMacUrl, cfg.VsCodeMacName, cfg.VsCodeMacHash);
            model.Platforms[Platform.MacArm64] = new VsCodeModel.PlatformData(cfg.VsCodeMacUrl, cfg.VsCodeMacName, cfg.VsCodeMacHash);

            return model;
        }

        private static bool VerifyPlatform(UpgradeConfig upgrade) =>
            GetCurrentPlatformType() == upgrade.InstallerType;

        private static string GetCurrentPlatformType()
        {
            if (OperatingSystem.IsWindows())
                return UpgradeConfig.WindowsInstallerType;

            if (OperatingSystem.IsMacOS())
                return PlatformUtils.CurrentPlatform == Platform.MacArm64
                    ? UpgradeConfig.MacArmInstallerType
                    : UpgradeConfig.MacInstallerType;

            if (OperatingSystem.IsLinux())
                return PlatformUtils.CurrentPlatform == Platform.LinuxArm64
                    ? UpgradeConfig.LinuxArm64InstallerType
                    : UpgradeConfig.LinuxInstallerType;

            return "Unknown";
        }

        private async Task DownloadAndPrepareVsCodeAsync(CancellationToken token)
        {
            Console.WriteLine("\nDownloading VS Code...");

            var currentPlatform = PlatformUtils.CurrentPlatform;
            var platformData = VsCodeModel.Platforms[currentPlatform];

            // Download VS Code using utility
            (MemoryStream stream, byte[] hash) = await VsCodeDownloadUtils.DownloadVsCodeForPlatformAsync(
                currentPlatform,
                platformData.DownloadUrl,
                progress => Console.WriteLine($"  Downloading VS Code... {progress:F2}%"),
                token);

            Console.WriteLine("  Verifying hash...");
            if (!hash.AsSpan().SequenceEqual(platformData.Sha256Hash))
            {
                throw new InvalidOperationException(
                    $"VS Code hash mismatch. Expected {Convert.ToHexString(platformData.Sha256Hash)}, got {Convert.ToHexString(hash)}");
            }

            // Prepare the model for extraction
            VsCodeDownloadUtils.PrepareVsCodeModelForInstallation(VsCodeModel, stream, currentPlatform);
            Console.WriteLine("  VS Code ready for installation");
        }
    }
}
