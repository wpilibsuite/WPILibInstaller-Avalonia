#nullable disable

using System.Text.Json.Serialization;

namespace WPILibInstaller.Models
{
    public class Extension
    {
        [JsonPropertyName("vsix")]
        public string Vsix { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("version")]
        public string Version { get; set; }
    }

    public class VsCodeConfig
    {
        public string VsCodeWindowsUrl { get; set; }
        public string VsCodeWindowsName { get; set; }
        public string VsCodeWindowsHash { get; set; }
        public long VsCodeWindowsSize { get; set; }
        public long VsCodeWindowsExtractedSize { get; set; }

        public string VsCodeWindowsArm64Url { get; set; }
        public string VsCodeWindowsArm64Name { get; set; }
        public string VsCodeWindowsArm64Hash { get; set; }
        public long VsCodeWindowsArm64Size { get; set; }
        public long VsCodeWindowsArm64ExtractedSize { get; set; }

        public string VsCodeMacUrl { get; set; }
        public string VsCodeMacName { get; set; }
        public string VsCodeMacHash { get; set; }
        public long VsCodeMacSize { get; set; }
        public long VsCodeMacExtractedSize { get; set; }


        public string VsCodeLinuxUrl { get; set; }
        public string VsCodeLinuxName { get; set; }
        public string VsCodeLinuxHash { get; set; }
        public long VsCodeLinuxSize { get; set; }
        public long VsCodeLinuxExtractedSize { get; set; }


        public string VsCodeLinuxArm64Url { get; set; }
        public string VsCodeLinuxArm64Name { get; set; }
        public string VsCodeLinuxArm64Hash { get; set; }
        public long VsCodeLinuxArm64Size { get; set; }
        public long VsCodeLinuxArm64ExtractedSize { get; set; }

        public string VsCodeVersion { get; set; }

        [JsonPropertyName("wpilibExtension")]
        public Extension WPILibExtension { get; set; }
        [JsonPropertyName("thirdPartyExtensions")]
        public Extension[] ThirdPartyExtensions { get; set; }
        [JsonPropertyName("javaExtensions")]
        public Extension[] JavaExtensions { get; set; }
        [JsonPropertyName("cppExtensions")]
        public Extension[] CppExtensions { get; set; }
        [JsonPropertyName("pythonExtensions")]
        public Extension[] PythonExtensions { get; set; }
    }
}
