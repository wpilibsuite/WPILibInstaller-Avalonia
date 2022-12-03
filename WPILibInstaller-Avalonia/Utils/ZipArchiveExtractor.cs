using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace WPILibInstaller.Utils
{
    public class ZipArchiveExtractor : IArchiveExtractor
    {
        private ZipArchive archive;
        private IEnumerator<ZipArchiveEntry> entries;


        public ZipArchiveExtractor(Stream stream)
        {
            this.archive = new ZipArchive(stream);
            TotalUncompressSize = (int)archive.Entries.Count;
            entries = archive.Entries.GetEnumerator();
        }

        public long TotalUncompressSize { get; }

        public string EntryKey => entries.Current.FullName;

        public int EntrySize => 1;

        public bool EntryIsDirectory => entries.Current.Name == "";

        public void Dispose()
        {
        }

        public bool MoveToNextEntry()
        {
            return entries.MoveNext();
        }

        public Task CopyToStreamAsync(Stream stream)
        {
            return entries.Current.Open().CopyToAsync(stream);
        }

        public bool EntryIsExecutable => false;
    }
}
