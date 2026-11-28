using System.Text.Json.Serialization;

namespace WPILibInstaller.Models
{
    /// <summary>
    /// The on-disk form of the installer's component selection. Only components that
    /// were actually installed are saved; disabled options are stored as false.
    /// </summary>
    public sealed class InstallSelectionSnapshot : IInstallSelectionValues
    {
        [JsonPropertyName("java")]
        public bool InstallJava { get; set; }

        [JsonPropertyName("cpp")]
        public bool InstallCpp { get; set; }

        [JsonPropertyName("python")]
        public bool InstallPython { get; set; }

        [JsonPropertyName("labview")]
        public bool InstallLabView { get; set; }

        [JsonPropertyName("wpilibTools")]
        public bool InstallWPILibTools { get; set; }

        [JsonPropertyName("elastic")]
        public bool InstallElastic { get; set; }

        [JsonPropertyName("advantageScope")]
        public bool InstallAdvantageScope { get; set; }

        public static InstallSelectionSnapshot FromModel(InstallSelectionModel model)
        {
            var snapshot = new InstallSelectionSnapshot();
            snapshot.CopyFrom(model);
            return snapshot;
        }

        public InstallSelectionModel ToModel()
        {
            var model = new InstallSelectionModel();
            model.CopyFrom(this);
            return model;
        }

        private void CopyFrom(IInstallSelectionValues source)
        {
            InstallSelectionValues.Copy(source, this);
        }
    }
}
