using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Threading;

namespace WPILibInstaller.CLI
{
    class CLIConfigurationProvider : IConfigurationProvider
    {
        private CLIConfigurationProvider(UpgradeConfig upgradeConfig, FullConfig fullConfig, JdkConfig jdkConfig, VsCodeConfig vsCodeConfig, IArchiveExtractor zipArchive, string installDirectory)
        {
            UpgradeConfig = upgradeConfig;
            FullConfig = fullConfig;
            JdkConfig = jdkConfig;
            VsCodeConfig = vsCodeConfig;
            ZipArchive = zipArchive;
            InstallDirectory = installDirectory;
        }

        public static async Task<CLIConfigurationProvider> From(string artifactsFile, string resourcesFile)
        {
            UpgradeConfig UpgradeConfig;
            FullConfig FullConfig;
            JdkConfig JdkConfig;
            VsCodeConfig VsCodeConfig;

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
                using (SHA256 SHA256 = SHA256Managed.Create())
                {
                    s = Convert.ToHexString(await SHA256.ComputeHashAsync(fileStream));
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


            entry = resourcesArchive.GetEntry("fullConfig.json");

            using (StreamReader reader = new StreamReader(entry!.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                FullConfig = JsonConvert.DeserializeObject<FullConfig>(configStr, new JsonSerializerSettings
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
            return new CLIConfigurationProvider(UpgradeConfig, FullConfig, JdkConfig, VsCodeConfig, ZipArchive, InstallDirectory);
        }

        public VsCodeModel VsCodeModel
        {
            get
            {
                VsCodeModel model = new VsCodeModel(VsCodeConfig.VsCodeVersion);
                model.Platforms.Add(Utils.Platform.Win32, new VsCodeModel.PlatformData(VsCodeConfig.VsCode32Url, VsCodeConfig.VsCode32Name));
                model.Platforms.Add(Utils.Platform.Win64, new VsCodeModel.PlatformData(VsCodeConfig.VsCode64Url, VsCodeConfig.VsCode64Name));
                model.Platforms.Add(Utils.Platform.Linux64, new VsCodeModel.PlatformData(VsCodeConfig.VsCodeLinuxUrl, VsCodeConfig.VsCodeLinuxName));
                model.Platforms.Add(Utils.Platform.Mac64, new VsCodeModel.PlatformData(VsCodeConfig.VsCodeMacUrl, VsCodeConfig.VsCodeMacName));
                return model;
            }
        }


        public IArchiveExtractor ZipArchive { get; private set; }

        public UpgradeConfig UpgradeConfig { get; private set; }

        public FullConfig FullConfig { get; private set; }

        public JdkConfig JdkConfig { get; private set; }

        public VsCodeConfig VsCodeConfig { get; private set; }

        public string InstallDirectory { get; private set; }
    }
}
