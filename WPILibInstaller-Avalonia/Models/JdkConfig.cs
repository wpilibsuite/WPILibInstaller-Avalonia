using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

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
