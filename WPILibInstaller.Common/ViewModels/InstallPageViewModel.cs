using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;

namespace WPILibInstaller.ViewModels
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    public partial class InstallPageViewModel : PageViewModelBase
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private readonly IViewModelResolver viewModelResolver;
        private readonly IToInstallProvider toInstallProvider;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IArchiveExtractionService archiveExtractionService;
        private readonly IVsCodeInstallationService vsCodeInstallationService;
        private readonly IToolInstallationService toolInstallationService;
        private readonly IShortcutService shortcutService;
        private readonly IInstallSelectionStore installSelectionStore;

        [ObservableProperty]
        public partial int Progress { get; set; }

        [ObservableProperty]
        public partial string Text { get; set; } = "";

        [ObservableProperty]
        public partial int ProgressTotal { get; set; }

        [ObservableProperty]
        public partial string TextTotal { get; set; } = "";

        public bool Succeeded { get; private set; }

        private readonly Task runInstallTask;

        private CancellationTokenSource? source;

        public InstallPageViewModel(
            IViewModelResolver viewModelResolver,
            IToInstallProvider toInstallProvider,
            IConfigurationProvider configurationProvider,
            IArchiveExtractionService archiveExtractionService,
            IVsCodeInstallationService vsCodeInstallationService,
            IToolInstallationService toolInstallationService,
            IShortcutService shortcutService,
            IInstallSelectionStore installSelectionStore)
            : base("", "")
        {
            this.viewModelResolver = viewModelResolver;
            this.toInstallProvider = toInstallProvider;
            this.configurationProvider = configurationProvider;
            this.archiveExtractionService = archiveExtractionService;
            this.vsCodeInstallationService = vsCodeInstallationService;
            this.toolInstallationService = toolInstallationService;
            this.shortcutService = shortcutService;
            this.installSelectionStore = installSelectionStore;
            runInstallTask = InstallFunc();

            async Task InstallFunc()
            {
                try
                {
                    await RunInstall();
                }
                catch (Exception e)
                {
                    viewModelResolver.ResolveMainWindow().HandleException(e);
                }
            }
        }

        [RelayCommand]
        public async Task CancelInstall()
        {
            source?.Cancel();
            await runInstallTask;
        }

        private Progress<InstallProgress> CreateProgressReporter()
        {
            return new Progress<InstallProgress>(progress =>
            {
                Progress = progress.Percentage;
                Text = progress.StatusText;
            });
        }

        private void SetOverallProgress(int percentage, string status)
        {
            ProgressTotal = percentage;
            TextTotal = status;
        }

        private async Task InstallSelected(CancellationToken token)
        {
            try
            {
                var progress = CreateProgressReporter();
                var selection = toInstallProvider.Model;
                var steps = new List<(string Label, Func<Task> Run)>();

                void Add(string label, Func<Task> run) => steps.Add((label, run));

                bool anyLanguage = selection.InstallJava || selection.InstallCpp;

                if (anyLanguage)
                {
                    Add("Stopping Gradle daemons", () => toolInstallationService.StopGradleDaemons(progress, token));
                }

                if (anyLanguage)
                {
                    Add("Extracting", () => archiveExtractionService.ExtractArchive(token, null, progress));
                }
                else if (selection.AnyToolsSelected)
                {
                    Add("Extracting selected tools", () => archiveExtractionService.ExtractSelectedTools(
                        selection.InstallWPILibTools,
                        selection.InstallElastic,
                        selection.InstallAdvantageScope,
                        token,
                        progress));
                }

                if (selection.InstallJava || selection.InstallCpp)
                {
                    Add("Installing Gradle", () => toolInstallationService.RunGradleSetup(progress));
                }

                if (selection.AnyToolsSelected)
                {
                    Add("Installing Tools", () => toolInstallationService.RunToolSetup(
                        selection.InstallWPILibTools,
                        selection.InstallElastic,
                        selection.InstallAdvantageScope,
                        progress));
                }

                if (selection.InstallCpp)
                {
                    Add("Installing CPP", () => toolInstallationService.RunCppSetup(progress));
                }

                if (selection.InstallJava || selection.InstallCpp)
                {
                    Add("Fixing Maven", () => toolInstallationService.RunMavenMetaDataFixer(progress));
                }

                if (selection.UsesVsCode)
                {
                    Add("Installing VS Code", () => vsCodeInstallationService.RunVsCodeSetup(token, progress));
                    Add("Configuring VS Code", () => vsCodeInstallationService.ConfigureVsCodeSettings());
                    Add("Installing VS Code Extensions", () => vsCodeInstallationService.RunVsCodeExtensionsSetup(progress));
                }

                if (selection.HasAnySelection)
                {
                    Add("Creating Shortcuts", () => shortcutService.RunShortcutCreator(token));
                }

                for (int i = 0; i < steps.Count; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    SetOverallProgress(i * 100 / steps.Count, steps[i].Label);
                    await steps[i].Run();
                }

                if (!token.IsCancellationRequested)
                {
                    SetOverallProgress(100, "Install complete");
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore, as we just want to continue.
            }
        }

        private async Task RunInstall()
        {
            source = new CancellationTokenSource();

            await Task.Yield();

            try
            {
                await InstallSelected(source.Token);
            }
            catch (OperationCanceledException)
            {
                // Ignore, as we just want to continue.
            }

            if (source.IsCancellationRequested)
            {
                Succeeded = false;
            }
            else
            {
                await installSelectionStore.SaveAsync(toInstallProvider.Model);
                CreateLegacyInstallSymlink();
                Succeeded = true;
            }

            await viewModelResolver.ResolveMainWindow().ExecuteGoNext();
        }

        private void CreateLegacyInstallSymlink()
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (string.IsNullOrWhiteSpace(userFolder))
                {
                    return;
                }

                CreateLegacyInstallSymlink(
                    userFolder,
                    configurationProvider.InstallDirectory,
                    configurationProvider.UpgradeConfig.wpilibYearFolder);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                // Leave existing user data alone if the compatibility link cannot be created.
            }
        }

        private static void CreateLegacyInstallSymlink(string userFolder, string installDirectory, string wpilibYearFolder)
        {
            var legacyPath = Path.Combine(userFolder, "wpilib");
            var legacyInfo = new DirectoryInfo(legacyPath);
            if (legacyInfo.LinkTarget != null)
            {
                return;
            }
            else if (File.Exists(legacyPath))
            {
                return;
            }

            Directory.CreateDirectory(legacyPath);

            var legacyFolderPath = Path.Combine(legacyPath, wpilibYearFolder);
            CreateOrUpdateSymlink(legacyFolderPath, Path.GetFullPath(installDirectory));
        }

        private static void CreateOrUpdateSymlink(string linkPath, string targetPath)
        {
            var linkInfo = new DirectoryInfo(linkPath);
            if (linkInfo.LinkTarget != null)
            {
                var linkParent = Path.GetDirectoryName(linkPath);
                if (string.IsNullOrWhiteSpace(linkParent))
                {
                    return;
                }

                var existingTarget = Path.GetFullPath(linkInfo.LinkTarget, linkParent);
                var comparison = OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (string.Equals(
                    Path.TrimEndingDirectorySeparator(existingTarget),
                    Path.TrimEndingDirectorySeparator(targetPath),
                    comparison))
                {
                    return;
                }

                Directory.Delete(linkPath);
            }
            else if (Directory.Exists(linkPath) || File.Exists(linkPath))
            {
                return;
            }

            Directory.CreateSymbolicLink(linkPath, targetPath);
        }

        public override PageViewModelBase MoveNext()
        {
            if (Succeeded)
            {
                return viewModelResolver.Resolve<FinalPageViewModel>();
            }

            return viewModelResolver.Resolve<CanceledPageViewModel>();
        }
    }
}
