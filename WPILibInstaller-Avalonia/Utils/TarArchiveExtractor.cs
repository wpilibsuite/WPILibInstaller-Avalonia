using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Tar;

namespace WPILibInstaller_Avalonia.Utils
{
    public class TarArchiveExtractor : IArchiveExtractor
    {
        private TarInputStream dataStream;
        private TarEntry currentEntry = null!;
        private byte[] headerStorage = new byte[512];
        private char[] charStorage = new char[512];

        public TarArchiveExtractor(Stream stream, int size)
        {
            TotalUncompressSize = size;
            var gzipStream = new GZipStream(stream, CompressionMode.Decompress);

            this.dataStream = new TarInputStream(gzipStream, Encoding.ASCII);
        }

        public int TotalUncompressSize { get; }

        public string EntryKey => currentEntry.Name;

        public int EntrySize => (int)currentEntry.Size;

        public bool EntryIsDirectory => currentEntry.IsDirectory;

        public void Dispose()
        {
            dataStream.Dispose();
        }

        public bool MoveToNextEntry()
        {
            var entry = dataStream.GetNextEntry();

            if (entry == null)
            {
                return false;
            }

            this.currentEntry = entry;
            return true;
        }

        public Task CopyToStreamAsync(Stream stream)
        {
            return dataStream.CopyToAsync(stream);
        }

        public bool EntryIsExecutable => (currentEntry.TarHeader.Mode & 1) != 0;
    }
}
