using WPILibInstaller.Models;

namespace WPILibInstaller.Interfaces
{
    public interface IInstallSizeCalculator
    {
        /// <summary>
        /// Returns the estimated installed footprint in bytes for the selected components,
        /// including second-stage extractions and copies, or null when the archive is unavailable.
        /// </summary>
        Task<long?> CalculateInstallSizeAsync(InstallSelectionModel selection, CancellationToken token = default);

        /// <summary>
        /// Returns the free bytes on the drive that contains the install directory,
        /// or null when the drive cannot be determined.
        /// </summary>
        long? GetAvailableSpaceBytes();
    }
}
