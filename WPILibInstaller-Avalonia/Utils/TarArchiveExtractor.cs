using System.IO;
using SharpCompress.Readers.Tar;

namespace WPILibInstaller_Avalonia.Utils
{
    public class TarArchiveExtractor : IArchiveExtractor
    {
        private TarReader reader;

        public TarArchiveExtractor(TarReader reader, int size)
        {
            TotalUncompressSize = size;
            this.reader = reader;
        }

        public int TotalUncompressSize { get; }

        public string EntryKey => reader.Entry.Key;

        public int EntrySize => (int)reader.Entry.Size;

        public bool EntryIsDirectory => reader.Entry.IsDirectory;

        public void Dispose()
        {

        }

        public bool MoveToNextEntry()
        {
            return reader.MoveToNextEntry();
        }

        public Stream OpenEntryStream()
        {
            return reader.OpenEntryStream();
        }
    }
}
