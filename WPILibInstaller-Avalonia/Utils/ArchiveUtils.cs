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

namespace WPILibInstaller_Avalonia.Utils
{
    public static class ArchiveUtils
    {
        public static IReader? OpenArchive(Stream stream)
        {
            try {
            IArchive archive = ZipArchive.Open(stream);
            Console.WriteLine("Open Archive");
            Console.WriteLine(archive.GetType());
            if (archive is ZipArchive)
            {
                return archive.ExtractAllEntries();
            }
            else if (archive is GZipArchive gza)
            {
                return TarReader.Open(gza.Entries.First().OpenEntryStream());
            }
            else
            {
                return null;
            }
            } catch (Exception e) {
                Console.WriteLine(e);
                return null;
            }
            
        }
    }
}
