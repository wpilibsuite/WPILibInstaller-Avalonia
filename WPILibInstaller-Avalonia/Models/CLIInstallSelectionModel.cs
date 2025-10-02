namespace WPILibInstaller.Models.CLI
{
    public class CLIInstallSelectionModel
    {
        public bool InstallCpp { get; set; } = false;
        public bool InstallGradle { get; set; } = false;
        public bool InstallJDK { get; set; } = false;
        public bool InstallTools { get; set; } = false;
        public bool InstallWPILibDeps { get; set; } = false;
        public bool InstallVsCode { get; set; } = false;
        public bool InstallAsAdmin { get; set; } = false;
        public bool InstallVsCodeExtensions { get; set; } = false;
    }
}
