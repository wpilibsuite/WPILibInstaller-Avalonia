using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;

namespace WPILibInstaller.ViewModels
{
    public class InstallPageViewModel : PageViewModelBase
    {
        private readonly IViewModelResolver viewModelResolver;
        private readonly IToInstallProvider toInstallProvider;
        private readonly IArchiveExtractionService archiveExtractionService;
        private readonly IVsCodeInstallationService vsCodeInstallationService;
        private readonly IToolInstallationService toolInstallationService;
        private readonly IShortcutService shortcutService;

        public int Progress { get; set; }
        public string Text { get; set; } = "";
        public int ProgressTotal { get; set; }
        public string TextTotal { get; set; } = "";

        public async Task UIUpdateTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                this.RaisePropertyChanged(nameof(Progress));
                this.RaisePropertyChanged(nameof(Text));
                this.RaisePropertyChanged(nameof(ProgressTotal));
                this.RaisePropertyChanged(nameof(TextTotal));
                try
                {
                    await Task.Delay(100, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public bool succeeded = false;

        private readonly Task runInstallTask;

        public InstallPageViewModel(
            IViewModelResolver viewModelResolver,
            IToInstallProvider toInstallProvider,
            IArchiveExtractionService archiveExtractionService,
            IVsCodeInstallationService vsCodeInstallationService,
            IToolInstallationService toolInstallationService,
            IShortcutService shortcutService,
            ICatchableButtonFactory buttonFactory)
            : base("", "")
        {
            this.viewModelResolver = viewModelResolver;
            this.toInstallProvider = toInstallProvider;
            this.archiveExtractionService = archiveExtractionService;
            this.vsCodeInstallationService = vsCodeInstallationService;
            this.toolInstallationService = toolInstallationService;
            this.shortcutService = shortcutService;
            CancelInstall = buttonFactory.CreateCatchableButton(CancelInstallFunc);
            runInstallTask = installFunc();

            async Task installFunc()
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

        private CancellationTokenSource? source;

        public ReactiveCommand<Unit, Unit> CancelInstall { get; }

        public async Task CancelInstallFunc()
        {
            source?.Cancel();
            await runInstallTask;
        }

        private IProgress<InstallProgress> CreateProgressReporter()
        {
            return new Progress<InstallProgress>(p =>
            {
                Progress = p.Percentage;
                Text = p.StatusText;
            });
        }

        private void SetOverallProgress(int percentage, string status)
        {
            ProgressTotal = percentage;
            TextTotal = status;
        }

        private async Task InstallTools(CancellationToken token)
        {
            try
            {
                var progress = CreateProgressReporter();
                do
                {
                    SetOverallProgress(0, "Extracting JDK and Tools");
                    await archiveExtractionService.ExtractJDKAndTools(token, progress);
                    if (token.IsCancellationRequested) break;

                    SetOverallProgress(33, "Installing Tools");
                    await toolInstallationService.RunToolSetup(progress);

                    SetOverallProgress(66, "Creating Shortcuts");
                    await shortcutService.RunShortcutCreator(token);
                } while (false);
            }
            catch (OperationCanceledException)
            {
                // Do nothing, we just want to ignore
            }
        }

        private async Task InstallEverything(CancellationToken token)
        {
            try
            {
                var progress = CreateProgressReporter();
                do
                {
                    SetOverallProgress(0, "Extracting");
                    await archiveExtractionService.ExtractArchive(token, null, progress);
                    if (token.IsCancellationRequested) break;

                    SetOverallProgress(11, "Installing Gradle");
                    await toolInstallationService.RunGradleSetup(progress);
                    if (token.IsCancellationRequested) break;

                    SetOverallProgress(22, "Installing Tools");
                    await toolInstallationService.RunToolSetup(progress);
                    if (token.IsCancellationRequested) break;

                    SetOverallProgress(33, "Installing CPP");
                    await toolInstallationService.RunCppSetup(progress);
                    if (token.IsCancellationRequested) break;

                    SetOverallProgress(44, "Fixing Maven");
                    await toolInstallationService.RunMavenMetaDataFixer(progress);
                    if (token.IsCancellationRequested) break;

                    SetOverallProgress(55, "Installing VS Code");
                    await vsCodeInstallationService.RunVsCodeSetup(token, progress);
                    if (token.IsCancellationRequested) break;

                    SetOverallProgress(66, "Configuring VS Code");
                    await vsCodeInstallationService.ConfigureVsCodeSettings();
                    if (token.IsCancellationRequested) break;

                    SetOverallProgress(77, "Installing VS Code Extensions");
                    await vsCodeInstallationService.RunVsCodeExtensionsSetup(progress);
                    if (token.IsCancellationRequested) break;

                    SetOverallProgress(88, "Creating Shortcuts");
                    await shortcutService.RunShortcutCreator(token);
                } while (false);
            }
            catch (OperationCanceledException)
            {
                // Do nothing, we just want to ignore
            }
        }

        private async Task RunInstall()
        {
            source = new CancellationTokenSource();

            await Task.Yield();

            var updateSource = new CancellationTokenSource();

            var updateTask = UIUpdateTask(updateSource.Token);

            try
            {
                if (toInstallProvider.Model.InstallTools)
                {
                    await InstallTools(source.Token);
                }
                else
                {
                    await InstallEverything(source.Token);
                }

                updateSource.Cancel();
                await updateTask;
            }
            catch (OperationCanceledException)
            {
                // Ignore, as we just want to continue
            }

            if (source.IsCancellationRequested)
            {
                succeeded = false;
            }
            else
            {
                succeeded = true;
            }

            viewModelResolver.ResolveMainWindow().ExecuteGoNext();
        }

        public override PageViewModelBase MoveNext()
        {
            if (succeeded)
            {
                return viewModelResolver.Resolve<FinalPageViewModel>();
            }
            else
            {
                return viewModelResolver.Resolve<CanceledPageViewModel>();
            }
        }
    }
}
