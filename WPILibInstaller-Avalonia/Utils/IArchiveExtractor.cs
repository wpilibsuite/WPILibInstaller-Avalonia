using System;
using System.IO;
using System.Threading.Tasks;

namespace WPILibInstaller.Utils
{
    public interface IArchiveExtractor : IDisposable
    {
        bool MoveToNextEntry();

        long TotalUncompressSize { get; }

        string EntryKey { get; }

        int EntrySize { get; }

        bool EntryIsDirectory { get; }

        bool EntryIsExecutable { get; }

        Task CopyToStreamAsync(Stream stream);
    }
}
