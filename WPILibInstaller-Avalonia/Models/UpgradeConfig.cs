#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace WPILibInstaller_Avalonia.Models
{
    public class MavenConfig
    {
        public string Folder { get; set; }
        public string MetaDataFixerExe { get; set; }
    }

    public class ToolsConfig
    {
        public string Folder { get; set; }
        public string UpdaterExe { get; set; }
    }

    public class UpgradeConfig
    {
        public string FrcYear { get; set; }
        public string InstallerType { get; set; }

        public const string Windows32InstallerType = "Windows32";
        public const string Windows64InstallerType = "Windows64";
        public const string LinuxInstallerType = "Linux";
        public const string MacInstallerType = "Mac";

        public MavenConfig Maven { get; set; }
        public ToolsConfig Tools { get; set; }
        public string PathFolder { get; set; }
    }
}
