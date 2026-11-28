using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;

namespace WPILibInstaller.ViewModels
{
    public partial class ConfigurationPageViewModel : PageViewModelBase, IToInstallProvider
    {
        private readonly IViewModelResolver viewModelResolver;
        private readonly IInstallSizeCalculator installSizeCalculator;

        private int sizeRefreshVersion;
        private long? estimatedInstallBytes;
        private long? availableDiskBytes;

        public InstallSelectionModel Model { get; }

        public override bool ForwardVisible => false;

        public bool CanRunAsAdmin { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [ObservableProperty]
        public partial string TotalInstallSizeText { get; set; } = "Estimated install size: calculating...";

        [ObservableProperty]
        public partial string AvailableDiskSpaceText { get; set; } = "Available disk space: calculating...";

        [ObservableProperty]
        public partial string DiskSpaceMessage { get; set; } = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanContinue))]
        public partial bool HasInsufficientDiskSpace { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanContinue))]
        public partial bool IsDiskSpaceCheckPending { get; set; } = true;

        public bool CanContinue => Model.HasAnySelection && !HasInsufficientDiskSpace && !IsDiskSpaceCheckPending;

        public ConfigurationPageViewModel(
            IViewModelResolver viewModelResolver,
            IInstallSelectionStore installSelectionStore,
            IInstallSizeCalculator installSizeCalculator)
            : base("Install", "Back")
        {
            this.viewModelResolver = viewModelResolver;
            this.installSizeCalculator = installSizeCalculator;
            Model = installSelectionStore.Load()?.ToModel() ?? new InstallSelectionModel();

            // Python and LabVIEW are still coming soon; keep them off even if a saved
            // selection contains them.
            Model.InstallPython = false;
            Model.InstallLabView = false;

            Model.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(CanContinue));
                RefreshInstallSize();
            };
            RefreshInstallSize();
        }

        [RelayCommand]
        public async Task InstallLocalUser()
        {
            if (!CanContinue)
            {
                return;
            }

            Model.InstallAsAdmin = false;
            await viewModelResolver.ResolveMainWindow().ExecuteGoNext();
        }

        [RelayCommand]
        public async Task InstallAdmin()
        {
            if (!CanContinue)
            {
                return;
            }

            Model.InstallAsAdmin = true;
            await viewModelResolver.ResolveMainWindow().ExecuteGoNext();
        }

        public override PageViewModelBase MoveNext()
        {
            if (Model.UsesVsCode)
            {
                return viewModelResolver.Resolve<VSCodePageViewModel>();
            }
            else
            {
                return viewModelResolver.Resolve<InstallPageViewModel>();
            }
        }

        private void RefreshInstallSize()
        {
            _ = RefreshInstallSizeAsync();
        }

        private async Task RefreshInstallSizeAsync()
        {
            int version = Interlocked.Increment(ref sizeRefreshVersion);
            IsDiskSpaceCheckPending = true;

            try
            {
                var estimateTask = installSizeCalculator.CalculateInstallSizeAsync(Model);
                availableDiskBytes = installSizeCalculator.GetAvailableSpaceBytes();
                long? estimate = await estimateTask;
                if (version != sizeRefreshVersion)
                {
                    return;
                }

                estimatedInstallBytes = estimate;
                UpdateDiskSpaceDisplay();
            }
            catch (Exception ex) when (ex is OperationCanceledException
                                          or InvalidOperationException
                                          or NullReferenceException)
            {
                if (version != sizeRefreshVersion)
                {
                    return;
                }

                estimatedInstallBytes = null;
                UpdateDiskSpaceDisplay();
            }
            finally
            {
                if (version == sizeRefreshVersion)
                {
                    IsDiskSpaceCheckPending = false;
                }
            }
        }

        private void UpdateDiskSpaceDisplay()
        {
            TotalInstallSizeText = estimatedInstallBytes.HasValue
                ? $"Estimated install size: {FormatSize(estimatedInstallBytes.Value)}"
                : "Estimated install size: unavailable";
            AvailableDiskSpaceText = availableDiskBytes.HasValue
                ? $"Available disk space: {FormatSize(availableDiskBytes.Value)}"
                : "Available disk space: unknown";

            if (estimatedInstallBytes.HasValue && availableDiskBytes.HasValue)
            {
                HasInsufficientDiskSpace = estimatedInstallBytes.Value > availableDiskBytes.Value;
                DiskSpaceMessage = HasInsufficientDiskSpace
                    ? $"Not enough disk space: {FormatSize(estimatedInstallBytes.Value)} required, {FormatSize(availableDiskBytes.Value)} available."
                    : "";
            }
            else
            {
                HasInsufficientDiskSpace = false;
                DiskSpaceMessage = "";
            }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024)
            {
                return $"{bytes} B";
            }

            double value = bytes;
            string[] units = ["B", "KB", "MB", "GB", "TB"];
            int unitIndex = 0;
            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return $"{value:0.#} {units[unitIndex]}";
        }
    }
}
