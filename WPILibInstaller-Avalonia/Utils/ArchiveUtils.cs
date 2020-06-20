using System;
using System.IO;
using SharpCompress.Archives.GZip;
using SharpCompress.Readers.Tar;
using System.Runtime.InteropServices;
using System.IO.Compression;

namespace WPILibInstaller_Avalonia.Utils
{
    public static class ArchiveUtils
    {
        public static IArchiveExtractor OpenArchive(Stream stream)
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

                var gzip = new GZipStream(stream, CompressionMode.Decompress);
                return new TarArchiveExtractor(TarReader.Open(gzip), uncompressedSize);
            }

            stream.Seek(0, SeekOrigin.Begin);
            var archive = new ZipArchive(stream);
            return new ZipArchiveExtractor(archive);

        }
    }
}
