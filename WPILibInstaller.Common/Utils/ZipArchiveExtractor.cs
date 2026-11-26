using System.IO.Compression;

namespace WPILibInstaller.Utils
{
    public class ZipArchiveExtractor : IArchiveExtractor
    {
        private ZipArchive archive;
        private IEnumerator<ZipArchiveEntry> entries;


        public ZipArchiveExtractor(Stream stream)
        {
            this.archive = new ZipArchive(stream);
            TotalUncompressSize = archive.Entries.Sum(entry => entry.Length);
            entries = archive.Entries.GetEnumerator();
        }

        public long TotalUncompressSize { get; }

        public string EntryKey => entries.Current.FullName;

        public long EntrySize => entries.Current.Length;

        public bool EntryIsDirectory => entries.Current.Name == "";

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public Task<bool> MoveToNextEntryAsync()
        {
            return Task.FromResult(entries.MoveNext());
        }

        public Task CopyToFileAsync(string path, CancellationToken token)
        {
            return entries.Current.ExtractToFileAsync(path, true, token);
        }

        public Stream OpenEntryStream()
        {
            return entries.Current.Open();
        }

        public bool EntryIsExecutable => false;
    }
}
