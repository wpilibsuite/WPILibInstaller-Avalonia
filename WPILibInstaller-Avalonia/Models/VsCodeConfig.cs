#nullable disable

using Newtonsoft.Json;

namespace WPILibInstaller.Models
{
    public class Extension
    {
        [JsonProperty("vsix")]
        public string Vsix { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class VsCodeConfig
    {
        public string VsCodeWindowsUrl { get; set; }
        public string VsCodeWindowsName { get; set; }
        public string VsCodeWindowsHash { get; set; }

        public string VsCodeMacUrl { get; set; }
        public string VsCodeMacName { get; set; }
        public string VsCodeMacHash { get; set; }

        public string VsCodeLinuxUrl { get; set; }
        public string VsCodeLinuxName { get; set; }
        public string VsCodeLinuxHash { get; set; }

        public string VsCodeLinuxArm64Url { get; set; }
        public string VsCodeLinuxArm64Name { get; set; }
        public string VsCodeLinuxArm64Hash { get; set; }

        public string VsCodeVersion { get; set; }

        [JsonProperty("wpilibExtension")]
        public Extension WPILibExtension { get; set; }
        [JsonProperty("thirdPartyExtensions")]
        public Extension[] ThirdPartyExtensions { get; set; }
    }
}
