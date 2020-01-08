using Avalonia.Controls;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using WPILibInstaller_Avalonia.Models;
using WPILibInstaller_Avalonia.Views;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class StartPageViewModel : PageViewModelBase, IRoutableViewModel
    {
        public IScreen HostScreen { get; }

        public string UrlPathSegment { get; } = "Start";

        private readonly MainWindow mainWindow;

        public StartPageViewModel(IScreen screen, MainWindow mainWindow)
            : base("Start", "Back")
        {
            this.mainWindow = mainWindow;
            HostScreen = screen;
        }


        private ZipArchive filesArchive;
        private VsCodeConfig vscodeConfig;
        private UpgradeConfig upgradeConfig;
        private JdkConfig jdkConfig;
        private FullConfig fullConfig;

        public async Task SelectSupportFiles()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AllowMultiple = false,
            };

            var selectedFiles = await openFileDialog.ShowAsync(mainWindow);
            if (selectedFiles.Length != 1) return;
            var file = selectedFiles[0];

            filesArchive = ZipFile.OpenRead(file);

            var entry = filesArchive.GetEntry("installUtils/vscodeConfig.json");

            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                var vsConfigStr = await reader.ReadToEndAsync();
                vscodeConfig = JsonConvert.DeserializeObject<VsCodeConfig>(vsConfigStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }

            entry = filesArchive.GetEntry("installUtils/jdkConfig.json");

            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                jdkConfig = JsonConvert.DeserializeObject<JdkConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }


            entry = filesArchive.GetEntry("installUtils/fullConfig.json");

            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                fullConfig = JsonConvert.DeserializeObject<FullConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }


            entry = filesArchive.GetEntry("installUtils/upgradeConfig.json");

            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                upgradeConfig = JsonConvert.DeserializeObject<UpgradeConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }


            entry = filesArchive.GetEntry("installUtils/jdkConfig.json");

            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                jdkConfig = JsonConvert.DeserializeObject<JdkConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }

            entry = filesArchive.GetEntry("installUtils/jdkConfig.json");

            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                jdkConfig = JsonConvert.DeserializeObject<JdkConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }
        }

        public VsCodeModel GetVsCodeModel()
        {
            VsCodeModel model = new VsCodeModel(vscodeConfig.VsCodeVersion);
            model.Platforms.Add(Utils.Platform.Win32, new VsCodeModel.PlatformData(vscodeConfig.VsCode32Url, vscodeConfig.VsCode32Name));
            model.Platforms.Add(Utils.Platform.Win64, new VsCodeModel.PlatformData(vscodeConfig.VsCode64Url, vscodeConfig.VsCode64Name));
            model.Platforms.Add(Utils.Platform.Linux64, new VsCodeModel.PlatformData(vscodeConfig.VsCodeLinuxUrl, vscodeConfig.VsCodeLinuxName));
            model.Platforms.Add(Utils.Platform.Mac64, new VsCodeModel.PlatformData(vscodeConfig.VsCodeMacUrl, vscodeConfig.VsCodeMacName));
            return model;
        }
    }
}
