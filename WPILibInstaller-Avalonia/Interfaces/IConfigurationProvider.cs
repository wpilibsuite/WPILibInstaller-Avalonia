using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using WPILibInstaller_Avalonia.Models;

namespace WPILibInstaller_Avalonia.Interfaces
{
    public interface IConfigurationProvider
    {
        VsCodeModel VsCodeModel { get; }

        ZipArchive ZipArchive { get; }

        UpgradeConfig UpgradeConfig { get; }
        FullConfig FullConfig { get; }

        JdkConfig JdkConfig { get; }

        string InstallDirectory { get; }
    }
}
