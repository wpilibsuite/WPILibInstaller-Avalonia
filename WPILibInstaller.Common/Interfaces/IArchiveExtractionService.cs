using WPILibInstaller.Models;

namespace WPILibInstaller.Interfaces
{
    public interface IArchiveExtractionService
    {
        Task ExtractArchive(CancellationToken token, string[]? filter = null, IProgress<InstallProgress>? progress = null);

        Task ExtractSelectedTools(bool wpilibTools, bool elastic, bool advantageScope, CancellationToken token, IProgress<InstallProgress>? progress = null);
    }
}
