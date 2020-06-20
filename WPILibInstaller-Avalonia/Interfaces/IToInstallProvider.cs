using WPILibInstaller_Avalonia.Models;

namespace WPILibInstaller_Avalonia.Interfaces
{
    public interface IToInstallProvider
    {
        InstallSelectionModel Model { get; }
    }
}
