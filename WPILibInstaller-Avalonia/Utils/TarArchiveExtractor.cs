﻿using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Tar;

namespace WPILibInstaller.Utils
{
    public class TarArchiveExtractor : IArchiveExtractor
    {
        private readonly TarInputStream dataStream;
        private TarEntry currentEntry = null!;

        public TarArchiveExtractor(Stream stream, long size)
        {
            TotalUncompressSize = size;
            var gzipStream = new GZipStream(stream, CompressionMode.Decompress);

            this.dataStream = new TarInputStream(gzipStream, Encoding.ASCII);
        }

        public long TotalUncompressSize { get; }

        public string EntryKey => currentEntry.Name;

        public int EntrySize => (int)currentEntry.Size;

        public bool EntryIsDirectory => currentEntry.IsDirectory;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
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
