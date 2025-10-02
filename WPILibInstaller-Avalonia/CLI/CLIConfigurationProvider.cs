using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.CLI
{
    class CLIConfigurationProvider : IConfigurationProvider
    {
        private CLIConfigurationProvider(UpgradeConfig upgradeConfig,
            FullConfig fullConfig, JdkConfig jdkConfig,
            VsCodeConfig vsCodeConfig, ChoreoConfig choreoConfig,
            AdvantageScopeConfig advantageScopeConfig, 
            ElasticConfig elasticConfig,
            IArchiveExtractor zipArchive, string installDirectory
        )
        {
            UpgradeConfig = upgradeConfig;
            FullConfig = fullConfig;
            JdkConfig = jdkConfig;
            VsCodeConfig = vsCodeConfig;
            ZipArchive = zipArchive;
            InstallDirectory = installDirectory;
            ChoreoConfig = choreoConfig;
            AdvantageScopeConfig = advantageScopeConfig;
            ElasticConfig = elasticConfig;
        }

        public static async Task<CLIConfigurationProvider> From(string artifactsFile, string resourcesFile)
        {
            UpgradeConfig UpgradeConfig;
            FullConfig FullConfig;
            JdkConfig JdkConfig;
            VsCodeConfig VsCodeConfig;
            ChoreoConfig ChoreoConfig;
            AdvantageScopeConfig AdvantageScopeConfig;
            ElasticConfig ElasticConfig;

            var publicFolder = Environment.GetEnvironmentVariable("PUBLIC");
            if (publicFolder == null)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    publicFolder = "C:\\Users\\Public";
                }
                else
                {
                    publicFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                }
            }

            FileStream fileStream = File.OpenRead(artifactsFile);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Read the original hash.
                string hash = File.ReadAllText(Path.Join(AppContext.BaseDirectory, "checksum.txt")).Trim();

                // Compute the hash of the file that exists.
                string s;
                using (var sha256 = SHA256.Create())
                {
                    s = Convert.ToHexString(await sha256.ComputeHashAsync(fileStream));
                }

                // Make sure they match.
                if (!s.Equals(hash.ToUpper()))
                {
                    throw new Exception("The artifacts file was damaged.");
                }
            }


            fileStream.Position = 0;
            var ZipArchive = ArchiveUtils.OpenArchive(fileStream);

            var resourcesArchive = ZipFile.OpenRead(resourcesFile);

            var entry = resourcesArchive.GetEntry("vscodeConfig.json");

            using (StreamReader reader = new StreamReader(entry!.Open()))
            {
                var vsConfigStr = await reader.ReadToEndAsync();
                VsCodeConfig = JsonConvert.DeserializeObject<VsCodeConfig>(vsConfigStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }

            entry = resourcesArchive.GetEntry("jdkConfig.json");

            using (StreamReader reader = new StreamReader(entry!.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                JdkConfig = JsonConvert.DeserializeObject<JdkConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }

            // CODE REVIEWERS: Please review this code.
            entry = resourcesArchive.GetEntry("choreoConfig.json");
            using (StreamReader reader = new StreamReader(entry!.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                ChoreoConfig = JsonConvert.DeserializeObject<ChoreoConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }

            // CODE REVIEWERS: please review this
            entry = resourcesArchive.GetEntry("advantageScopeConfig.json");
            using (StreamReader reader = new StreamReader(entry!.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                AdvantageScopeConfig = JsonConvert.DeserializeObject<AdvantageScopeConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }

            entry = resourcesArchive.GetEntry("fullConfig.json");

            using (StreamReader reader = new StreamReader(entry!.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                FullConfig = JsonConvert.DeserializeObject<FullConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }

            entry = resourcesArchive.GetEntry("elasticConfig.json");

            using (StreamReader reader = new StreamReader(entry!.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                ElasticConfig = JsonConvert.DeserializeObject<ElasticConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }

            entry = resourcesArchive.GetEntry("upgradeConfig.json");

            using (StreamReader reader = new StreamReader(entry!.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                UpgradeConfig = JsonConvert.DeserializeObject<UpgradeConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }

            var InstallDirectory = Path.Combine(publicFolder, "wpilib", UpgradeConfig.FrcYear);
            return new CLIConfigurationProvider(
                UpgradeConfig, FullConfig, JdkConfig, VsCodeConfig, 
                ChoreoConfig, AdvantageScopeConfig, ElasticConfig, ZipArchive, 
                InstallDirectory
            );
        }

        public VsCodeModel VsCodeModel
        {
            get
            {
                VsCodeModel model = new VsCodeModel(VsCodeConfig.VsCodeVersion);
                model.Platforms.Add(Utils.Platform.Win64, new VsCodeModel.PlatformData(VsCodeConfig.VsCodeWindowsUrl, VsCodeConfig.VsCodeWindowsName, VsCodeConfig.VsCodeWindowsHash));
                model.Platforms.Add(Utils.Platform.Linux64, new VsCodeModel.PlatformData(VsCodeConfig.VsCodeLinuxUrl, VsCodeConfig.VsCodeLinuxName, VsCodeConfig.VsCodeLinuxHash));
                model.Platforms.Add(Utils.Platform.LinuxArm64, new VsCodeModel.PlatformData(VsCodeConfig.VsCodeLinuxArm64Url, VsCodeConfig.VsCodeLinuxArm64Name, VsCodeConfig.VsCodeLinuxArm64Hash));
                model.Platforms.Add(Utils.Platform.Mac64, new VsCodeModel.PlatformData(VsCodeConfig.VsCodeMacUrl, VsCodeConfig.VsCodeMacName, VsCodeConfig.VsCodeMacHash));
                model.Platforms.Add(Utils.Platform.MacArm64, new VsCodeModel.PlatformData(VsCodeConfig.VsCodeMacUrl, VsCodeConfig.VsCodeMacName, VsCodeConfig.VsCodeMacHash));
                return model;
            }
        }


        public IArchiveExtractor ZipArchive { get; private set; }

        public UpgradeConfig UpgradeConfig { get; private set; }

        public FullConfig FullConfig { get; private set; }

        public JdkConfig JdkConfig { get; private set; }

        public VsCodeConfig VsCodeConfig { get; private set; }

        public ChoreoConfig ChoreoConfig { get; private set; }

        public ElasticConfig ElasticConfig { get; private set; }

        public AdvantageScopeConfig AdvantageScopeConfig { get; private set; }

        public string InstallDirectory { get; private set; }
    }
}
