namespace WPILibInstaller.Models
{
    public class InstallSelectionModel
    {
        public bool InstallTools { get; set; } = false;

        public bool InstallEverything { get; set; } = true;

        public bool InstallAsAdmin { get; set; } = false;
    }
}
