using WPILibInstaller_Avalonia.Models;

namespace WPILibInstaller_Avalonia.Interfaces
{
    public interface IVsCodeInstallLocationProvider
    {
        VsCodeModel Model { get; }
    }
}
