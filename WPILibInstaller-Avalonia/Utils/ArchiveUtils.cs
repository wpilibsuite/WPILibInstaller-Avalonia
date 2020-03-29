using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Archives.GZip;
using SharpCompress.Readers;
using SharpCompress.Readers.Tar;
using System.Runtime.InteropServices;
namespace WPILibInstaller_Avalonia.Utils
{
    public static class ArchiveUtils
    {
        public static (IReader reader, int size, IArchive archive) OpenArchive(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            if (GZipArchive.IsGZipFile(stream))
            {
                // Seek to end, grab size
                stream.Seek(-4, SeekOrigin.End);
                Span<int> intSpan = stackalloc int[1];

                stream.Read(MemoryMarshal.AsBytes(intSpan));

                int uncompressedSize = intSpan[0];

                stream.Seek(0, SeekOrigin.Begin);
                var gzip = GZipArchive.Open(stream);
                return (TarReader.Open(gzip.Entries.First().OpenEntryStream()), uncompressedSize, gzip);
            }

            stream.Seek(0, SeekOrigin.Begin);
            IArchive archive = ZipArchive.Open(stream);
            return (archive.ExtractAllEntries(), (int)archive.TotalUncompressSize, archive);
            
        }
    }
}
