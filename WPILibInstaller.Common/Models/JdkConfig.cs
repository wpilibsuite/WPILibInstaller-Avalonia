#nullable disable

using Newtonsoft.Json;

namespace WPILibInstaller.Models
{
    public class JdkConfig
    {
        [JsonProperty("tarFile")]
        public string TarFile { get; set; }
        [JsonProperty("folder")]
        public string Folder { get; set; }
    }
}
