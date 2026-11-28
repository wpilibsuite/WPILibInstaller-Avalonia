#nullable disable

using System.Text.Json.Serialization;

namespace WPILibInstaller.Models
{
    public class LabVIEWConfig
    {
        [JsonPropertyName("folder")]
        public string Folder { get; set; }
    }
}
