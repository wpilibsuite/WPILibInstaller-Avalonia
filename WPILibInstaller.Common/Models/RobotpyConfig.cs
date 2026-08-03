#nullable disable

using System.Text.Json.Serialization;

namespace WPILibInstaller.Models
{
    public class RobotpyConfig
    {
        [JsonPropertyName("whlFile")]
        public string WhlFile { get; set; }
        [JsonPropertyName("folder")]
        public string Folder { get; set; }
    }
}
