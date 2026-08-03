#nullable disable

using System.Text.Json.Serialization;

namespace WPILibInstaller.Models
{
    public class PythonConfig
    {
        [JsonPropertyName("exeFile")]
        public string ExeFile { get; set; }
        [JsonPropertyName("pkgFile")]
        public string PkgFile { get; set; }
        [JsonPropertyName("folder")]
        public string Folder { get; set; }
    }
}
