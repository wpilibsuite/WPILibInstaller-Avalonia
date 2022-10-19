﻿using System;
using System.Collections.Generic;
using System.IO;
using WPILibInstaller.Utils;

namespace WPILibInstaller.Models
{
    public class VsCodeModel : IDisposable
    {
        public class PlatformData
        {
            public string DownloadUrl { get; }
            public string NameInZip { get; }
            private readonly byte[] hash;
            public ReadOnlySpan<byte> Md5Hash => hash;

            public PlatformData(string downloadUrl, string nameInZip, string md5Hash)
            {
                this.DownloadUrl = downloadUrl;
                this.NameInZip = nameInZip;
                this.hash = Convert.FromHexString(md5Hash);
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

        public bool InstallingVsCode => ToExtractArchive != null || ToExtractArchiveMacOs != null;

        public bool InstallExtensions => AlreadyInstalled || InstallingVsCode;

        public void Dispose()
        {
            ToExtractArchive?.Dispose();
            ToExtractArchiveMacOs?.Dispose();
        }
    }
}
