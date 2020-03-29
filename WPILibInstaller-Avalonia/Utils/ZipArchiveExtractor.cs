using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using SharpCompress.Readers.Zip;

namespace WPILibInstaller_Avalonia.Utils
{
    public class ZipArchiveExtractor : IArchiveExtractor
    {
        private ZipArchive archive;
        private IEnumerator<ZipArchiveEntry> entries;


        public ZipArchiveExtractor(ZipArchive archive)
        {
            this.archive = archive;
            TotalUncompressSize = (int)archive.Entries.Count;
            entries = archive.Entries.GetEnumerator();
        }

        public int TotalUncompressSize { get; }

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

        public Stream OpenEntryStream()
        {
            return entries.Current.Open();
        }
    }
}
