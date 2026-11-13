#nullable disable

using System.Text.Json.Serialization;

namespace WPILibInstaller.Models
{
    public class RobotpyConfig
    {
        [JsonPropertyName("folder")]
        public string Folder { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }
}
