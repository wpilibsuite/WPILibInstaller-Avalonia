using System.Text.Json.Serialization;

namespace WPILibInstaller.Models
{
    /// <summary>
    /// The on-disk form of the installer's component selection. Only components that
    /// were actually installed are saved; disabled options are stored as false.
    /// </summary>
    public sealed class InstallSelectionSnapshot
    {
        [JsonPropertyName("java")]
        public bool Java { get; set; }

        [JsonPropertyName("cpp")]
        public bool Cpp { get; set; }

        [JsonPropertyName("python")]
        public bool Python { get; set; }

        [JsonPropertyName("labview")]
        public bool LabView { get; set; }

        [JsonPropertyName("wpilibTools")]
        public bool WPILibTools { get; set; }

        [JsonPropertyName("elastic")]
        public bool Elastic { get; set; }

        [JsonPropertyName("advantageScope")]
        public bool AdvantageScope { get; set; }

        public static InstallSelectionSnapshot FromModel(InstallSelectionModel model)
        {
            return new InstallSelectionSnapshot
            {
                Java = model.InstallJava,
                Cpp = model.InstallCpp,
                Python = model.InstallPython,
                LabView = model.InstallLabView,
                WPILibTools = model.InstallWPILibTools,
                Elastic = model.InstallElastic,
                AdvantageScope = model.InstallAdvantageScope,
            };
        }

        public InstallSelectionModel ToModel()
        {
            var model = new InstallSelectionModel
            {
                InstallJava = Java,
                InstallCpp = Cpp,
                InstallPython = Python,
                InstallLabView = LabView,
                InstallWPILibTools = WPILibTools,
                InstallElastic = Elastic,
                InstallAdvantageScope = AdvantageScope,
            };
            return model;
        }
    }
}
