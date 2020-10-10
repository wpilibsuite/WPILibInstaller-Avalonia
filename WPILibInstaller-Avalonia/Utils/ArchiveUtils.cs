using System;
using System.IO;
using System.Runtime.InteropServices;
using System.IO.Compression;
using System.Threading.Tasks;

namespace WPILibInstaller_Avalonia.Utils
{
    public static class ArchiveUtils
    {
        public static IArchiveExtractor OpenArchive(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            // Read first 3 bytes, check for first 3 bytes 1F 8B 08
            Span<byte> header = stackalloc byte[3];

            int bytesRead = stream.Read(header);

            if (bytesRead != 3)
            {
                throw new InvalidDataException("Empty Stream?");
            }

            if (header[0] == 0x1F && header[1] == 0x8B && header[2] == 0x08)
            {
                // Seek to end, grab size
                stream.Seek(-4, SeekOrigin.End);
                Span<int> intSpan = stackalloc int[1];

                stream.Read(MemoryMarshal.AsBytes(intSpan));

                int uncompressedSize = intSpan[0];

                stream.Seek(0, SeekOrigin.Begin);

                return new TarArchiveExtractor(stream, uncompressedSize);
            }

            stream.Seek(0, SeekOrigin.Begin);
            return new ZipArchiveExtractor(stream);

        }
    }
}
