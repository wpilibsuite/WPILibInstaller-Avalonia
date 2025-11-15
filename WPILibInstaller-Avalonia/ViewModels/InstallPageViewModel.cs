using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using WPILibInstaller.InstallTasks;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Interfaces.Observer;

namespace WPILibInstaller.ViewModels
{
    public class InstallPageViewModel : PageViewModelBase, IObserver
    {
        private readonly IViewModelResolver viewModelResolver;
        private readonly IToInstallProvider toInstallProvider;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IVsCodeInstallLocationProvider vsInstallProvider;
        private readonly IProgramWindow programWindow;

        private Func<Task<bool>> uacTimeoutCallback;

        public int Progress { get; set; }
        public string Text { get; set; } = "";
        public int ProgressTotal { get; set; }
        public string TextTotal { get; set; } = "";


        public void Update(ISubject subject)
        {
            if ((subject as InstallTask) != null)
            {
                InstallTask task = (subject as InstallTask)!;
                Progress = task.Progress;
                Text = task.Text;
                ProgressTotal = task.ProgressTotal;
                TextTotal = task.TextTotal;
            }
        }

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

        public InstallPageViewModel(IViewModelResolver viewModelResolver, IToInstallProvider toInstallProvider, IConfigurationProvider configurationProvider, IVsCodeInstallLocationProvider vsInstallProvider,
            IProgramWindow programWindow, ICatchableButtonFactory buttonFactory)
            : base("", "")
        {
            this.viewModelResolver = viewModelResolver;
            this.toInstallProvider = toInstallProvider;
            this.configurationProvider = configurationProvider;
            this.vsInstallProvider = vsInstallProvider;
            this.programWindow = programWindow;
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

            // Define what to do if UAC times out during shortcut creation (windows)
            uacTimeoutCallback = async () =>
            {
                var results = await MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(new MsBox.Avalonia.Dto.MessageBoxStandardParams
                {
                    ContentTitle = "UAC Prompt Cancelled",
                    ContentMessage = "UAC Prompt Cancelled or Timed Out. Would you like to retry?",
                    Icon = MsBox.Avalonia.Enums.Icon.Info,
                    ButtonDefinitions = MsBox.Avalonia.Enums.ButtonEnum.YesNo
                }).ShowWindowDialogAsync(programWindow.Window);
                return (results == MsBox.Avalonia.Enums.ButtonResult.Yes);
            };
        }

        private CancellationTokenSource? source;

        public ReactiveCommand<Unit, Unit> CancelInstall { get; }

        public async Task CancelInstallFunc()
        {
            source?.Cancel();
            await runInstallTask;
        }

        private async Task foundRunningExeHandler()
        {
            // For handling if a runing JDK process was found during archive extraction
            string msg = "Running JDK processes have been found. Installation cannot continue. Please restart your computer, and rerun this installer without running anything else (including VS Code)";
            await MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(new MsBox.Avalonia.Dto.MessageBoxStandardParams
            {
                ContentTitle = "JDKs Running",
                ContentMessage = msg,
                Icon = MsBox.Avalonia.Enums.Icon.Error,
                ButtonDefinitions = MsBox.Avalonia.Enums.ButtonEnum.Ok
            }).ShowWindowDialogAsync(programWindow.Window);
            throw new InvalidOperationException(msg);
        }

        private async Task ExtractJDKAndTools(CancellationToken token)
        {
            ExtractArchiveTask task = new ExtractArchiveTask(
                configurationProvider, new[] {
                configurationProvider.JdkConfig.Folder + "/",
                configurationProvider.UpgradeConfig.Tools.Folder + "/",
                configurationProvider.AdvantageScopeConfig.Folder + "/",
                configurationProvider.ElasticConfig.Folder + "/",
                "installUtils/", "icons"}
            );

            task.Attach(this); // Subscribe to progress changes
            try
            {
                await task.Execute(token);
            }

            // Handle if a running exe was found
            catch (FoundRunningExeException)
            {
                await foundRunningExeHandler();
            }

            task.Detach(this); // Unsubscribe from progress changes
        }

        private async Task InstallTools(CancellationToken token)
        {
            try
            {
                do
                {
                    ProgressTotal = 0;
                    TextTotal = "Extracting JDK and Tools";
                    await ExtractJDKAndTools(token);
                    if (token.IsCancellationRequested) break;

                    // Tool setup
                    {
                        ProgressTotal = 33;
                        TextTotal = "Installing Tools";
                        var task = new ToolSetupTask(
                            configurationProvider
                        );
                        task.Attach(this);
                        await task.Execute(token);
                        if (token.IsCancellationRequested) break;
                    }

                    // Creating shortcuts 
                    {
                        ProgressTotal = 66;
                        TextTotal = "Creating Shortcuts";
                        var task = new ShortcutCreatorTask(
                            vsInstallProvider.Model, configurationProvider,
                            toInstallProvider.Model.InstallAsAdmin,
                            toInstallProvider.Model.InstallEverything
                        );
                        task.Attach(this);
                        // Define what to do if UAC times out (windows)
                        task.uacTimeoutCallback = uacTimeoutCallback;

                        try
                        {
                            await task.Execute(token);
                        }
                        // Handle shortcut creator failing
                        catch (ShortcutCreationFailedException err)
                        {
                            await MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(new MsBox.Avalonia.Dto.MessageBoxStandardParams
                            {
                                ContentTitle = "Shortcut Creation Failed",
                                ContentMessage = $"Shortcut creation failed with error code {err.Message}",
                                Icon = MsBox.Avalonia.Enums.Icon.Warning,
                                ButtonDefinitions = MsBox.Avalonia.Enums.ButtonEnum.Ok
                            }).ShowWindowDialogAsync(programWindow.Window);
                        }

                        if (token.IsCancellationRequested) break;
                    }
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
                do
                {
                    // Extract the archive
                    {
                        ProgressTotal = 0;
                        TextTotal = "Extracting";
                        var task = new ExtractArchiveTask(
                            configurationProvider, null
                        );

                        task.Attach(this); // Subscribe to progress changes
                        try
                        {
                            await task.Execute(token);
                        }

                        // Handle if a running exe was found
                        catch (FoundRunningExeException)
                        {
                            await foundRunningExeHandler();
                        }
                        if (token.IsCancellationRequested)
                            break;

                        task.Detach(this); // Unsubscribe from progress changes
                    }

                    // Install Gradle
                    {
                        ProgressTotal = 11;
                        TextTotal = "Installing Gradle";
                        var task = new GradleSetupTask(
                            configurationProvider
                        );
                        task.Attach(this);
                        await task.Execute(token);
                        if (token.IsCancellationRequested) break;
                        task.Detach(this);
                    }

                    // Tool setup
                    {
                        ProgressTotal = 22;
                        TextTotal = "Installing Tools";
                        var task = new ToolSetupTask(
                            configurationProvider
                        );
                        task.Attach(this);
                        await task.Execute(token);
                        if (token.IsCancellationRequested) break;
                    }

                    // C++ setup
                    {
                        ProgressTotal = 33;
                        TextTotal = "Installing C++";
                        var task = new CppSetupTask(
                            configurationProvider
                        );
                        task.Attach(this);
                        await task.Execute(token);
                        if (token.IsCancellationRequested) break;
                    }

                    // Maven fixing 
                    {
                        ProgressTotal = 44;
                        TextTotal = "Fixing Maven";
                        var task = new MavenMetaDataFixerTask(
                            configurationProvider
                        );
                        task.Attach(this);
                        await task.Execute(token);
                        if (token.IsCancellationRequested) break;
                    }

                    // Installing vscode 
                    {
                        ProgressTotal = 55;
                        TextTotal = "Installing VS Code";
                        var task = new VsCodeSetupTask(
                            vsInstallProvider.Model, configurationProvider
                        );
                        task.Attach(this);
                        await task.Execute(token);
                        if (token.IsCancellationRequested) break;
                    }

                    // Configuring vscode 
                    {
                        ProgressTotal = 66;
                        TextTotal = "Configuring VS Code";
                        var task = new ConfigureVsCodeSettingsTask(
                            vsInstallProvider.Model, configurationProvider
                        );
                        task.Attach(this);
                        await task.Execute(token);
                        if (token.IsCancellationRequested) break;
                    }

                    // Installing vscode extensions 
                    {
                        ProgressTotal = 77;
                        TextTotal = "Installing VS Code Extensions";
                        var task = new VsCodeExtensionsSetupTask(
                            vsInstallProvider.Model, configurationProvider
                        );
                        task.Attach(this);
                        await task.Execute(token);
                        if (token.IsCancellationRequested) break;
                    }

                    // Creating shortcuts 
                    {
                        ProgressTotal = 88;
                        TextTotal = "Creating Shortcuts";
                        var task = new ShortcutCreatorTask(
                            vsInstallProvider.Model, configurationProvider,
                            toInstallProvider.Model.InstallAsAdmin,
                            toInstallProvider.Model.InstallEverything
                        );
                        task.Attach(this);

                        // Define what to do if UAC times out (windows)
                        task.uacTimeoutCallback = uacTimeoutCallback;

                        try
                        {
                            await task.Execute(token);
                        }

                        // Handle shortcut creator failing
                        catch (ShortcutCreationFailedException err)
                        {
                            await MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(new MsBox.Avalonia.Dto.MessageBoxStandardParams
                            {
                                ContentTitle = "Shortcut Creation Failed",
                                ContentMessage = $"Shortcut creation failed with error code {err.Message}",
                                Icon = MsBox.Avalonia.Enums.Icon.Warning,
                                ButtonDefinitions = MsBox.Avalonia.Enums.ButtonEnum.Ok
                            }).ShowWindowDialogAsync(programWindow.Window);
                        }

                        if (token.IsCancellationRequested) break;
                    }
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

            await viewModelResolver.ResolveMainWindow().ExecuteGoNext();
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
