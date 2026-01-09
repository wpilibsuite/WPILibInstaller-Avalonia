using System;
using System.Threading;
using System.Threading.Tasks;
using WPILibInstaller.Models;

namespace WPILibInstaller.Interfaces
{
    public interface IArchiveExtractionService
    {
        Task ExtractArchive(CancellationToken token, string[]? filter, IProgress<InstallProgress>? progress = null);
        Task ExtractJDKAndTools(CancellationToken token, IProgress<InstallProgress>? progress = null);
    }
}
