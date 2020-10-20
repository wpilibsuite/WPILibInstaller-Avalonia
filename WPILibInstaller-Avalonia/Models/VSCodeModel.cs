using System;
using System.Collections.Generic;
using System.IO;
using WPILibInstaller_Avalonia.Utils;

namespace WPILibInstaller_Avalonia.Models
{
    public class VsCodeModel : IDisposable
    {
        public class PlatformData
        {
            public string DownloadUrl { get; set; }
            public string NameInZip { get; set; }

            public PlatformData(string downloadUrl, string nameInZip)
            {
                this.DownloadUrl = downloadUrl;
                this.NameInZip = nameInZip;
            }
        }

        public string VSCodeVersion { get; set; }
        public Dictionary<Platform, PlatformData> Platforms { get; } = new Dictionary<Platform, PlatformData>();

        public IArchiveExtractor? ToExtractArchive { get; set; }

        public Stream? ToExtractArchiveMacOs { get; set; }

        public bool AlreadyInstalled { get; set; }

        public VsCodeModel(string vscodeVersion)
        {
            VSCodeVersion = vscodeVersion;
        }

        public void Dispose()
        {
            ToExtractArchive?.Dispose();
            ToExtractArchiveMacOs?.Dispose();
        }
    }
}
