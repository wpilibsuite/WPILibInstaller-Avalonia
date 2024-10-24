#nullable disable

using Newtonsoft.Json;

namespace WPILibInstaller.Models
{
    public class AdvantageScopeConfig
    {
        [JsonProperty("zipFile")]
        public string ZipFile { get; set; }
        [JsonProperty("folder")]
        public string Folder { get; set; }
    }
}
