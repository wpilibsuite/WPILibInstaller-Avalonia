using System;
using System.IO;

namespace WPILibInstaller_Avalonia.Utils
{
    public interface IArchiveExtractor : IDisposable
    {
        bool MoveToNextEntry();

        int TotalUncompressSize { get; }

        string EntryKey { get; }

        int EntrySize { get; }

        bool EntryIsDirectory { get; }

        Stream OpenEntryStream();
    }
}
