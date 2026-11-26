namespace WPILibInstaller.Utils
{
    public interface IArchiveExtractor : IDisposable
    {
        Task<bool> MoveToNextEntryAsync();

        long TotalUncompressSize { get; }

        string EntryKey { get; }

        long EntrySize { get; }

        bool EntryIsDirectory { get; }

        bool EntryIsExecutable { get; }

        Task CopyToFileAsync(string path, CancellationToken token);

        Stream OpenEntryStream();
    }
}
