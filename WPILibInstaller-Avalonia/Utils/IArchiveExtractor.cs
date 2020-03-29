using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SharpCompress.Common;

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
