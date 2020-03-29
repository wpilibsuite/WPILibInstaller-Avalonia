using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using SharpCompress.Archives;
using SharpCompress.Readers;
using WPILibInstaller_Avalonia.Models;
using WPILibInstaller_Avalonia.Utils;

namespace WPILibInstaller_Avalonia.Interfaces
{
    public interface IConfigurationProvider
    {
        VsCodeModel VsCodeModel { get; }

        IArchiveExtractor ZipArchive { get; }

        UpgradeConfig UpgradeConfig { get; }
        FullConfig FullConfig { get; }

        JdkConfig JdkConfig { get; }

        string InstallDirectory { get; }
    }
}
