using WPILibInstaller.Models;

namespace WPILibInstaller.Interfaces
{
    public interface IInstallSelectionStore
    {
        InstallSelectionSnapshot? Load();

        Task SaveAsync(InstallSelectionModel model, CancellationToken token = default);
    }
}
