#nullable disable

using System.Text.Json.Serialization;

namespace WPILibInstaller.Models
{
    public class PythonConfig
    {
        [JsonPropertyName("folder")]
        public string Folder { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("archiveFolder")]
        public string ArchiveFolder { get; set; }

        [JsonPropertyName("archiveFile")]
        public string ArchiveFile { get; set; }

        [JsonPropertyName("uvFolder")]
        public string UvFolder { get; set; }
    }
}
