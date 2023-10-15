using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ReactiveUI;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.ViewModels
{
    public class StartPageViewModel : PageViewModelBase, IConfigurationProvider
    {

        private readonly IProgramWindow programWindow;
        private readonly IViewModelResolver viewModelResolver;
        private readonly IMainWindowViewModel refresher;

        public override bool ForwardVisible => forwardVisible;
        private bool forwardVisible = false;
        public string VerString => verString;

        private readonly string verString = $"0.0.0";

        private bool missingHash = false;

        public void Initialize()
        {
            var baseDir = AppContext.BaseDirectory;

            var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "zip" : "tar.gz";

            bool foundResources = false;
            bool foundSupport = false;

            // Enumerate all files in base dir
            foreach (var file in Directory.EnumerateFiles(baseDir))
            {
                if (file.EndsWith($"{verString}-resources.zip"))
                {
                    _ = SelectResourceFilesWithFile(file);
                    foundResources = true;
                }
                else if (file.EndsWith($"{verString}-artifacts.{extension}"))
                {
                    _ = SelectSupportFilesWithFile(file);
                    foundSupport = true;
                }
            }

            // Assume app is running in a translocated process.
            if ((!foundResources || !foundSupport) && RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                && Directory.Exists("/Volumes/WPILibInstaller"))
            {
                baseDir = Path.GetFullPath("/Volumes/WPILibInstaller");
                foreach (var file in Directory.EnumerateFiles(baseDir))
                {
                    if (!foundResources && file.EndsWith($"{verString}-resources.zip"))
                    {
                        _ = SelectResourceFilesWithFile(file);
                        foundResources = true;
                    }
                    else if (!foundSupport && file.EndsWith($"{verString}-artifacts.{extension}"))
                    {
                        _ = SelectSupportFilesWithFile(file);
                        foundSupport = true;
                    }
                }
            }

            // Look beside the .app
            if ((!foundResources || !foundSupport) && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Go back 3 directories to back out of mac package
                baseDir = Path.GetFullPath(Path.Join(baseDir, "..", "..", ".."));
                foreach (var file in Directory.EnumerateFiles(baseDir))
                {
                    if (!foundResources && file.EndsWith($"{verString}-resources.zip"))
                    {
                        _ = SelectResourceFilesWithFile(file);
                        foundResources = true;
                    }
                    else if (!foundSupport && file.EndsWith($"{verString}-artifacts.{extension}"))
                    {
                        _ = SelectSupportFilesWithFile(file);
                        foundSupport = true;
                    }
                }
            }
        }

        public StartPageViewModel(IMainWindowViewModel mainRefresher, IProgramWindow mainWindow, IViewModelResolver viewModelResolver,
            ICatchableButtonFactory buttonFactory)
            : base("Start", "")
        {
            try
            {
                var rootDirectory = Directory.GetDirectoryRoot(Environment.GetFolderPath(Environment.SpecialFolder.Personal));

                var driveInfo = new DriveInfo(rootDirectory);

                if (driveInfo.AvailableFreeSpace < 3L * 1000L * 1000L * 1000L)
                {
                    ;
                    // Fail
                }
            }
            catch
            {
                // Do nothing if we couldn't determine the drive
            }

            SelectSupportFiles = buttonFactory.CreateCatchableButton(SelectSupportFilesFunc);
            SelectResourceFiles = buttonFactory.CreateCatchableButton(SelectResourceFilesFunc);

            this.programWindow = mainWindow;
            this.viewModelResolver = viewModelResolver;
            refresher = mainRefresher;

            var baseDir = AppContext.BaseDirectory;

            try
            {
                verString = File.ReadAllText(Path.Join(baseDir, "WPILibInstallerVersion.txt")).Trim();
            }
            catch
            {
            }
        }

        public bool MissingSupportFiles
        {
            get => missingSupportFiles;
            set
            {
                this.RaiseAndSetIfChanged(ref missingSupportFiles, value);
                this.RaisePropertyChanged(nameof(MissingEitherFile));
            }
        }

        public bool MissingHash
        {
            get => missingHash;
            set
            {
                this.RaiseAndSetIfChanged(ref missingHash, value);
            }
        }

        public bool MissingEitherFile => MissingSupportFiles || MissingResourceFiles;

        private bool missingSupportFiles = true;

        public bool MissingResourceFiles
        {
            get => missingResourceFiles;
            set
            {
                this.RaiseAndSetIfChanged(ref missingResourceFiles, value);
                this.RaisePropertyChanged(nameof(MissingEitherFile));
            }
        }

        private bool missingResourceFiles = true;


        public ReactiveCommand<Unit, Unit> SelectSupportFiles { get; }
        public ReactiveCommand<Unit, Unit> SelectResourceFiles { get; }

        private async Task<bool> SelectResourceFilesWithFile(string file)
        {
            var zipArchive = ZipFile.OpenRead(file);

            var entry = zipArchive.GetEntry("vscodeConfig.json");

            if (entry == null)
            {
                return false;
            }

            using (StreamReader reader = new StreamReader(entry!.Open()))
            {
                var vsConfigStr = await reader.ReadToEndAsync();
                VsCodeConfig = JsonConvert.DeserializeObject<VsCodeConfig>(vsConfigStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }

            entry = zipArchive.GetEntry("jdkConfig.json");

            using (StreamReader reader = new StreamReader(entry!.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                JdkConfig = JsonConvert.DeserializeObject<JdkConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }


            entry = zipArchive.GetEntry("fullConfig.json");

            using (StreamReader reader = new StreamReader(entry!.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                FullConfig = JsonConvert.DeserializeObject<FullConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }


            entry = zipArchive.GetEntry("upgradeConfig.json");

            using (StreamReader reader = new StreamReader(entry!.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                UpgradeConfig = JsonConvert.DeserializeObject<UpgradeConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }

            string? neededInstaller = CheckInstallerType();
            if (neededInstaller == null)
            {
                MissingResourceFiles = false;
                forwardVisible = !MissingEitherFile && !MissingHash;
                refresher.RefreshForwardBackProperties();

                return true;
            }
            else
            {
                viewModelResolver.ResolveMainWindow().HandleException(new IncorrectPlatformException(neededInstaller, UpgradeConfig.InstallerType));
                return false;
            }
        }


        private string? CheckInstallerType()
        {
            // TODO Handle Arm someday
            if (OperatingSystem.IsWindows())
            {
                if (UpgradeConfig.InstallerType != UpgradeConfig.WindowsInstallerType)
                {
                    return UpgradeConfig.WindowsInstallerType;
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                if (PlatformUtils.CurrentPlatform == Platform.MacArm64)
                {
                    if (UpgradeConfig.InstallerType != UpgradeConfig.MacArmInstallerType)
                    {
                        return UpgradeConfig.MacArmInstallerType;
                    }
                }
                else
                {
                    if (UpgradeConfig.InstallerType != UpgradeConfig.MacInstallerType)
                    {
                        return UpgradeConfig.MacInstallerType;
                    }
                }

            }
            else if (OperatingSystem.IsLinux())
            {
                if (UpgradeConfig.InstallerType != UpgradeConfig.LinuxInstallerType)
                {
                    return UpgradeConfig.LinuxInstallerType;
                }
            }
            else
            {
                return "Unknown";
            }
            return null;
        }

        public async Task SelectResourceFilesFunc()
        {
            var file = await programWindow.ShowFilePicker("Select Resource File", "zip", Environment.GetFolderPath(Environment.SpecialFolder.Personal));

            if (file == null)
            {
                return;
            }

            await SelectResourceFilesWithFile(file);
        }

        private async Task<bool> SelectSupportFilesWithFile(string file)
        {
            FileStream fileStream = File.OpenRead(file);
            MissingSupportFiles = false;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                MissingHash = true;
                // Read the original hash.
                string hash = File.ReadAllText(Path.Join(AppContext.BaseDirectory, "checksum.txt")).Trim();

                // Compute the hash of the file that exists.
                string s;
                using (SHA256 SHA256 = SHA256.Create())
                {
                    s = Convert.ToHexString(await SHA256.ComputeHashAsync(fileStream));
                }

                // Make sure they match.
                if (!s.Equals(hash.ToUpper()))
                {
                    viewModelResolver.ResolveMainWindow().HandleException(new Exception("The artifacts file was damaged.\nThis is either caused by a bad download,\nor on macOS you originally download the wrong dmg\nand its still mounted. Make sure to eject\nall dmg's and try again (And maybe reboot)."));
                    return false;
                }
                MissingHash = false;
            }

            MissingSupportFiles = false;
            forwardVisible = !MissingEitherFile && !MissingHash;
            refresher.RefreshForwardBackProperties();

            fileStream.Position = 0;
            ZipArchive = ArchiveUtils.OpenArchive(fileStream);

            return true;
        }

        public async Task SelectSupportFilesFunc()
        {
            var file = await programWindow.ShowFilePicker("Select Artifact File", RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "zip" : "gz", Environment.GetFolderPath(Environment.SpecialFolder.Personal));

            if (file == null)
            {
                return;
            }

            await SelectSupportFilesWithFile(file);
        }

        public VsCodeModel VsCodeModel
        {
            get
            {
                VsCodeModel model = new VsCodeModel(VsCodeConfig.VsCodeVersion);
                model.Platforms.Add(Utils.Platform.Win64, new VsCodeModel.PlatformData(VsCodeConfig.VsCodeWindowsUrl, VsCodeConfig.VsCodeWindowsName, VsCodeConfig.VsCodeWindowsHash));
                model.Platforms.Add(Utils.Platform.Linux64, new VsCodeModel.PlatformData(VsCodeConfig.VsCodeLinuxUrl, VsCodeConfig.VsCodeLinuxName, VsCodeConfig.VsCodeLinuxHash));
                model.Platforms.Add(Utils.Platform.Mac64, new VsCodeModel.PlatformData(VsCodeConfig.VsCodeMacUrl, VsCodeConfig.VsCodeMacName, VsCodeConfig.VsCodeMacHash));
                model.Platforms.Add(Utils.Platform.MacArm64, new VsCodeModel.PlatformData(VsCodeConfig.VsCodeMacUrl, VsCodeConfig.VsCodeMacName, VsCodeConfig.VsCodeMacHash));
                return model;
            }
        }

        public string InstallDirectory
        {
            get
            {
                var publicFolder = Environment.GetEnvironmentVariable("PUBLIC");
                if (publicFolder == null)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        publicFolder = "C:\\Users\\Public";
                    }
                    else
                    {
                        publicFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    }
                }
                return Path.Combine(publicFolder, "wpilib", UpgradeConfig.FrcYear);
            }
        }

        public override PageViewModelBase MoveNext()
        {
            return viewModelResolver.Resolve<ConfigurationPageViewModel>();
        }

        public IArchiveExtractor ZipArchive { get; private set; } = null!;

        public UpgradeConfig UpgradeConfig { get; private set; } = null!;

        public FullConfig FullConfig { get; private set; } = null!;

        public JdkConfig JdkConfig { get; private set; } = null!;

        public VsCodeConfig VsCodeConfig { get; private set; } = null!;

    }
}
