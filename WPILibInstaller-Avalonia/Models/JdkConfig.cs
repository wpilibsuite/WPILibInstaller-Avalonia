#nullable disable

using Newtonsoft.Json;

namespace WPILibInstaller_Avalonia.Models
{
    public class JdkConfig
    {
        [JsonProperty("tarFile")]
        public string TarFile { get; set; }
        [JsonProperty("folder")]
        public string Folder { get; set; }
    }
}
