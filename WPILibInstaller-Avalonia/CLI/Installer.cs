using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.CLI {
    public class CLIConfigurationProvider : IConfigurationProvider {
        public CLIConfigurationProvider(string[] args) {
            this.UpgradeConfig = new UpgradeConfig();
            this.FullConfig = new FullConfig();
            this.JdkConfig = new JdkConfig();
            this.VsCodeConfig = new VsCodeConfig();
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
            this.InstallDirectory = Path.Combine(publicFolder, "wpilib", UpgradeConfig.FrcYear);

            FileStream fileStream = File.OpenRead("");
            //if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            //{
                //// Read the original hash.
                //string hash = File.ReadAllText(Path.Join(AppContext.BaseDirectory, "checksum.txt")).Trim();

                //// Compute the hash of the file that exists.
                //string s;
                //using (SHA256 SHA256 = SHA256Managed.Create())
                //{
                    //s = Convert.ToHexString(await SHA256.ComputeHashAsync(fileStream));
                //}

                //// Make sure they match.
                //if (!s.Equals(hash.ToUpper()))
                //{
                    //throw new Exception("The artifacts file was damaged.");
                //}
            //}


            fileStream.Position = 0;
            this.ZipArchive = ArchiveUtils.OpenArchive(fileStream);
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

    public class Installer {
        private readonly IConfigurationProvider configurationProvider;

        public Installer(string[] args) {
            this.configurationProvider = new CLIConfigurationProvider(args);
        }

        public void Install() {
            Console.WriteLine("Extracting");
            Console.WriteLine("Installing Gradle");
            Console.WriteLine("Installing Tools");
            Console.WriteLine("Installing CPP");
            Console.WriteLine("Fixing Maven");
            Console.WriteLine("Installing VS Code");
            Console.WriteLine("Configuring VS Code");
            Console.WriteLine("Installing VS Code Extensions");
            Console.WriteLine("Creating Shortcuts");
        }
    }
}
