using System;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;


namespace WPILibInstaller.InstallTasks
{
    public class ConfigureVsCodeSettingsTask : InstallTask
    {

        private readonly VsCodeModel vsCodeModel;

        public ConfigureVsCodeSettingsTask(
            VsCodeModel pVsCodeModel,
            IConfigurationProvider pConfigurationProvider
        )
            : base(pConfigurationProvider)
        {
            vsCodeModel = pVsCodeModel;
        }

        public override async Task Execute(CancellationToken token)
        {
            if (!vsCodeModel.InstallExtensions) return;

            var dataPath = await SetVsCodePortableMode();

            var settingsDir = Path.Combine(dataPath, "user-data", "User");
            var settingsFile = Path.Combine(settingsDir, "settings.json");

            var homePath = configurationProvider.InstallDirectory;

            var codeFolder = Path.Combine(homePath, configurationProvider.UpgradeConfig.PathFolder);

            try
            {
                Directory.CreateDirectory(codeFolder);
            }
            catch (IOException)
            {

            }

            try
            {
                Directory.CreateDirectory(settingsDir);
            }
            catch (IOException)
            {

            }

            JObject settingsJson = new JObject();
            if (File.Exists(settingsFile))
            {
                settingsJson = (JObject)JsonConvert.DeserializeObject(await File.ReadAllTextAsync(settingsFile))!;
            }

            SetIfNotSet("java.jdt.ls.java.home", Path.Combine(homePath, "jdk"), settingsJson);
            SetIfNotSet("extensions.autoUpdate", false, settingsJson);
            SetIfNotSet("extensions.autoCheckUpdates", false, settingsJson);
            SetIfNotSet("extensions.ignoreRecommendations", true, settingsJson);
            SetIfNotSet("update.mode", "none", settingsJson);
            SetIfNotSet("update.showReleaseNotes", false, settingsJson);
            SetIfNotSet("java.completion.matchCase", "off", settingsJson);

            string os;
            string path_seperator;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                os = "windows";
                path_seperator = ";";
            }
            else if (OperatingSystem.IsMacOS())
            {
                os = "osx";
                path_seperator = ":";
            }
            else
            {
                os = "linux";
                path_seperator = ":";
            }

            if (!settingsJson.ContainsKey("terminal.integrated.env." + os))
            {
                JObject terminalProps = new JObject
                {
                    ["JAVA_HOME"] = Path.Combine(homePath, "jdk"),
                    ["PATH"] = Path.Combine(homePath, "jdk", "bin") + path_seperator + "${env:PATH}"
                };

                settingsJson["terminal.integrated.env." + os] = terminalProps;

            }
            else
            {
                JToken terminalEnv = settingsJson["terminal.integrated.env." + os]!;
                terminalEnv["JAVA_HOME"] = Path.Combine(homePath, "jdk");
                JToken? path = terminalEnv["PATH"];
                if (path == null)
                {
                    terminalEnv["PATH"] = Path.Combine(homePath, "jdk", "bin") + path_seperator + "${env:PATH}";
                }
                else
                {
                    var binPath = Path.Combine(homePath, "jdk", "bin");
                    if (!path.ToString().Contains(binPath))
                    {
                        path = binPath + path_seperator + path;
                        terminalEnv["PATH"] = path;
                    }
                }
            }

            if (settingsJson.ContainsKey("java.configuration.runtimes"))
            {
                JArray javaConfigEnv = (JArray)settingsJson["java.configuration.runtimes"]!;
                Boolean javaFound = false;
                foreach (JToken result in javaConfigEnv)
                {
                    JToken? name = result["name"];
                    if (name != null)
                    {
                        if (name.ToString().Equals("JavaSE-17"))
                        {
                            result["path"] = Path.Combine(homePath, "jdk");
                            result["default"] = true;
                            javaFound = true;
                        }
                        else
                        {
                            result["default"] = false;
                        }
                    }
                }
                if (!javaFound)
                {
                    JObject javaConfigProp = new JObject
                    {
                        ["name"] = "JavaSE-17",
                        ["path"] = Path.Combine(homePath, "jdk"),
                        ["default"] = true
                    };
                    javaConfigEnv.Add(javaConfigProp);
                    settingsJson["java.configuration.runtimes"] = javaConfigEnv;
                }
            }
            else
            {
                JArray javaConfigProps = new JArray();
                JObject javaConfigProp = new JObject
                {
                    ["name"] = "JavaSE-17",
                    ["path"] = Path.Combine(homePath, "jdk"),
                    ["default"] = true
                };
                javaConfigProps.Add(javaConfigProp);
                settingsJson["java.configuration.runtimes"] = javaConfigProps;
            }

            var serialized = JsonConvert.SerializeObject(settingsJson, Formatting.Indented);
            await File.WriteAllTextAsync(settingsFile, serialized);
        }

        private ValueTask<string> SetVsCodePortableMode()
        {
            string portableFolder = Path.Combine(configurationProvider.InstallDirectory, "vscode");

            var currentPlatform = PlatformUtils.CurrentPlatform;
            switch (currentPlatform)
            {
                case Platform.Win64:
                    portableFolder = Path.Combine(portableFolder, "data");
                    break;
                case Platform.MacArm64:
                case Platform.Mac64:
                    portableFolder = Path.Combine(portableFolder, "code-portable-data");
                    break;
                case Platform.Linux64:
                    portableFolder = Path.Combine(portableFolder, "VSCode-linux-x64", "data");
                    break;
                case Platform.LinuxArm64:
                    portableFolder = Path.Combine(portableFolder, "VSCode-linux-arm64", "data");
                    break;
                default:
                    throw new PlatformNotSupportedException("Invalid platform");
            }

            try
            {
                Directory.CreateDirectory(portableFolder);
            }
            catch (IOException)
            {

            }

            return new ValueTask<string>(portableFolder);
        }

        private static void SetIfNotSet(string key, object value, JObject settingsJson)
        {
            if (!settingsJson.ContainsKey(key))
            {
                settingsJson[key] = JToken.FromObject(value);
            }
        }
    }
}
