using Avalonia.Controls;
using Newtonsoft.Json;
using ReactiveUI;
using SharpCompress.Archives;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using WPILibInstaller_Avalonia.Interfaces;
using WPILibInstaller_Avalonia.Models;
using WPILibInstaller_Avalonia.Utils;
using WPILibInstaller_Avalonia.Views;
using static WPILibInstaller_Avalonia.Utils.ReactiveExtensions;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class StartPageViewModel : PageViewModelBase, IConfigurationProvider
    {

        private readonly IProgramWindow programWindow;
        private readonly IDependencyInjection di;
        private readonly IMainWindowViewModelRefresher refresher;

        public override bool ForwardVisible => forwardVisible;
        private bool forwardVisible = false;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public StartPageViewModel(IMainWindowViewModelRefresher mainRefresher, IProgramWindow mainWindow, IDependencyInjection di)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
            : base("Start", "Back")
        {

            SelectSupportFiles = CreateCatchableButton(SelectSupportFilesFunc);
            SelectResourceFiles = CreateCatchableButton(SelectResourceFilesFunc);

            this.programWindow = mainWindow;
            this.di = di;
            refresher = mainRefresher;
        }

        public bool MissingSupportFiles
        {
            get => missingSupportFiles;
            set
            {
                this.RaiseAndSetIfChanged(ref missingSupportFiles, value);
                this.RaisePropertyChanged(nameof(MissingEitherFile));
            }
        }

        public bool MissingEitherFile => MissingSupportFiles || MissingResourceFiles;

        private bool missingSupportFiles = false;

        public bool MissingResourceFiles
        {
            get => missingResourceFiles;
            set
            {
                this.RaiseAndSetIfChanged(ref missingResourceFiles, value);
                this.RaisePropertyChanged(nameof(MissingEitherFile));
            }
        }

        private bool missingResourceFiles = true;
        private VsCodeConfig vscodeConfig;

        public ReactiveCommand<Unit, Unit> SelectSupportFiles { get; }
        public ReactiveCommand<Unit, Unit> SelectResourceFiles { get; }

        public async Task SelectResourceFilesFunc()
        {
            var file = await programWindow.ShowFilePicker("Select Resource File", Environment.GetFolderPath(Environment.SpecialFolder.Personal));

            if (file == null)
            {
                return;
            }

            var zipArchive = ZipFile.OpenRead(file);

            var entry = zipArchive.GetEntry("vscodeConfig.json");

            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                var vsConfigStr = await reader.ReadToEndAsync();
                vscodeConfig = JsonConvert.DeserializeObject<VsCodeConfig>(vsConfigStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }

            entry = zipArchive.GetEntry("jdkConfig.json");

            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                JdkConfig = JsonConvert.DeserializeObject<JdkConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }


            entry = zipArchive.GetEntry("fullConfig.json");

            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                FullConfig = JsonConvert.DeserializeObject<FullConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }


            entry = zipArchive.GetEntry("upgradeConfig.json");

            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                var configStr = await reader.ReadToEndAsync();
                UpgradeConfig = JsonConvert.DeserializeObject<UpgradeConfig>(configStr, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error
                }) ?? throw new InvalidOperationException("Not Valid");
            }

            MissingResourceFiles = false;
            forwardVisible = !MissingEitherFile;
            refresher.RefreshForwardBackProperties();
        }

        public async Task SelectSupportFilesFunc()
        {
            var file = await programWindow.ShowFilePicker("Select Support File", Environment.GetFolderPath(Environment.SpecialFolder.Personal));

            ZipArchive = SharpCompress.Archives.Zip.ZipArchive.Open(file);

            MissingSupportFiles = false;
            forwardVisible = !MissingEitherFile;
            refresher.RefreshForwardBackProperties();
        }

        public VsCodeModel VsCodeModel
        {
            get
            {
                VsCodeModel model = new VsCodeModel(vscodeConfig.VsCodeVersion);
                model.Platforms.Add(Utils.Platform.Win32, new VsCodeModel.PlatformData(vscodeConfig.VsCode32Url, vscodeConfig.VsCode32Name));
                model.Platforms.Add(Utils.Platform.Win64, new VsCodeModel.PlatformData(vscodeConfig.VsCode64Url, vscodeConfig.VsCode64Name));
                model.Platforms.Add(Utils.Platform.Linux64, new VsCodeModel.PlatformData(vscodeConfig.VsCodeLinuxUrl, vscodeConfig.VsCodeLinuxName));
                model.Platforms.Add(Utils.Platform.Mac64, new VsCodeModel.PlatformData(vscodeConfig.VsCodeMacUrl, vscodeConfig.VsCodeMacName));
                return model;
            }
        }

        public string InstallDirectory
        {
            get
            {
                var publicFolder = Environment.GetEnvironmentVariable("PUBLIC");
                if (publicFolder == null)
                {
                    publicFolder = "C:\\Users\\Public";
                }
                return Path.Combine(publicFolder, "wpilib", UpgradeConfig.FrcYear);
            }
        }

        public override PageViewModelBase MoveNext()
        {
            return di.Resolve<VSCodePageViewModel>();
        }

        public IArchive ZipArchive { get; private set; }

        public UpgradeConfig UpgradeConfig { get; private set; }

        public FullConfig FullConfig { get; private set; }

        public JdkConfig JdkConfig { get; private set; }
    }
}
