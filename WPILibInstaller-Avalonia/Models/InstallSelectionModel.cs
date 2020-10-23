﻿namespace WPILibInstaller.Models
{
    public class InstallSelectionModel
    {
        public bool InstallVsCode { get; set; } = true;
        public bool InstallCpp { get; set; } = true;
        public bool InstallGradle { get; set; } = true;
        public bool InstallJDK { get; set; } = true;
        public bool InstallTools { get; set; } = true;
        public bool InstallWPILibDeps { get; set; } = true;
        public bool InstallVsCodeExtensions { get; set; } = true;

        public bool InstallAsAdmin { get; set; } = false;
    }
}
