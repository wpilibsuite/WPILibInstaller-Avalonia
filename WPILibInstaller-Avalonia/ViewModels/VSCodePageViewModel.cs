﻿using System;
using System.IO;
using System.IO.Compression;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MessageBox.Avalonia;
using ReactiveUI;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;
using static WPILibInstaller.Utils.ArchiveUtils;

namespace WPILibInstaller.ViewModels
{
    public class VSCodePageViewModel : PageViewModelBase, IVsCodeInstallLocationProvider
    {

        public override bool ForwardVisible => forwardVisible;

        private bool forwardVisible = false;

        private void SetLocalForwardVisible(bool value)
        {
            forwardVisible = value;
            refresher.RefreshForwardBackProperties();
        }

        public bool EnableSelectionButtons
        {
            get => enableSelectionButtons;
            set
            {
                this.RaiseAndSetIfChanged(ref enableSelectionButtons, value);
            }
        }

        private bool enableSelectionButtons = true;

        public string SingleDownloadText
        {
            get => singleDownloadText;
            set => this.RaiseAndSetIfChanged(ref singleDownloadText, value);
        }

        public string SkipVsCodeText
        {
            get => skipVsCodeText;
            set => this.RaiseAndSetIfChanged(ref skipVsCodeText, value);
        }

        public string AllDownloadText
        {
            get => allDownloadText;
            set => this.RaiseAndSetIfChanged(ref allDownloadText, value);
        }

        public string SelectText
        {
            get => selectText;
            set => this.RaiseAndSetIfChanged(ref selectText, value);
        }

        private string singleDownloadText = "Download for this computer only\n(fastest)";
        private string skipVsCodeText = "Skip and don't use VS Code\n(NOT RECOMMENDED)";
        private string allDownloadText = "Create VS Code zip to share with\nother computers/OSes for offline\ninstall";
        private string selectText = "Select existing VS Code zip for\noffline install on this computer";

        public double ProgressBar1
        {
            get => progressBar1;
            set => this.RaiseAndSetIfChanged(ref progressBar1, value);
        }

        private double progressBar1 = 0;

        public bool ProgressBar1Visible
        {
            get => progressBar1Visible;
            set => this.RaiseAndSetIfChanged(ref progressBar1Visible, value);
        }

        private bool progressBar1Visible = false;

        public double ProgressBar2
        {
            get => progressBar2;
            set => this.RaiseAndSetIfChanged(ref progressBar2, value);
        }

        private double progressBar2 = 0;

        public bool ProgressBarAllVisible
        {
            get => progressBarAllVisible;
            set => this.RaiseAndSetIfChanged(ref progressBarAllVisible, value);
        }

        private bool progressBarAllVisible = false;

        public double ProgressBar3
        {
            get => progressBar3;
            set => this.RaiseAndSetIfChanged(ref progressBar3, value);
        }

        private double progressBar3 = 0;

        public double ProgressBar4
        {
            get => progressBar4;
            set => this.RaiseAndSetIfChanged(ref progressBar4, value);
        }

        private double progressBar4 = 0;

        public string DoneText
        {
            get => doneText;
            set => this.RaiseAndSetIfChanged(ref doneText, value);
        }

        private string doneText = "";

        public VsCodeModel Model { get; }

        private readonly IProgramWindow programWindow;
        private readonly IMainWindowViewModel refresher;
        private readonly IViewModelResolver viewModelResolver;

        public VSCodePageViewModel(IMainWindowViewModel mainRefresher, IProgramWindow programWindow, IConfigurationProvider modelProvider, IViewModelResolver viewModelResolver,
            ICatchableButtonFactory buttonFactory)
            : base("Next", "Back")
        {
            SkipVsCode = buttonFactory.CreateCatchableButton(SkipVsCodeFunc);
            DownloadSingleVsCode = buttonFactory.CreateCatchableButton(DownloadSingleVSCodeFunc);
            DownloadVsCode = buttonFactory.CreateCatchableButton(DownloadVsCodeFunc);
            SelectVsCode = buttonFactory.CreateCatchableButton(SelectVsCodeFunc);


            this.refresher = mainRefresher;
            this.programWindow = programWindow;
            Model = modelProvider.VsCodeModel;
            this.viewModelResolver = viewModelResolver;

            refresher.RefreshForwardBackProperties();

            // Check to see if VS Code is already installed
            var rootPath = modelProvider.InstallDirectory;
            var vscodePath = Path.Join(rootPath, "vscode");
            if (Directory.Exists(vscodePath))
            {
                DoneText = "VS Code already Installed. You can either download to reinstall, or click Next to skip";
                SetLocalForwardVisible(true);
                Model.AlreadyInstalled = true;
            }
        }

        public ReactiveCommand<Unit, Unit> SkipVsCode { get; }
        public ReactiveCommand<Unit, Unit> SelectVsCode { get; }
        public ReactiveCommand<Unit, Unit> DownloadVsCode { get; }
        public ReactiveCommand<Unit, Unit> DownloadSingleVsCode { get; }

        private async Task SkipVsCodeFunc()
        {
            if (Model.AlreadyInstalled)
            {
                await viewModelResolver.ResolveMainWindow().ExecuteGoNext();
                return;
            }

            var result = await MessageBoxManager.GetMessageBoxStandardWindow("Confirmation",
                "Are you sure you want to skip installing VS Code?\nA WPILib VS Code install was not detected.",
                icon: MessageBox.Avalonia.Enums.Icon.None, @enum: MessageBox.Avalonia.Enums.ButtonEnum.YesNo).ShowDialog(programWindow.Window);

            if (result == MessageBox.Avalonia.Enums.ButtonResult.Yes)
            {
                await viewModelResolver.ResolveMainWindow().ExecuteGoNext();
            }
        }

        private async Task SelectVsCodeFunc()
        {
            var file = await programWindow.ShowFilePicker("Select VS Code Installer ZIP", "zip");
            if (file == null)
            {
                // No need to error, user explicitly canceled.
                return;
            }
            try
            {
                FileStream fs = new FileStream(file, FileMode.Open);
                using System.IO.Compression.ZipArchive archive = new System.IO.Compression.ZipArchive(fs);
                var currentPlatform = PlatformUtils.CurrentPlatform;
                var entry = archive.GetEntry(Model.Platforms[currentPlatform].NameInZip);
                MemoryStream ms = new MemoryStream(100000000);
                await entry!.Open().CopyToAsync(ms);

                if (OperatingSystem.IsMacOS())
                {
                    Model.ToExtractArchiveMacOs = ms;
                }
                else
                {
                    Model.ToExtractArchive = OpenArchive(ms);
                }
            }
            catch
            {
                await MessageBoxManager.GetMessageBoxStandardWindow("Error",
                    "Correct VS Code not found in archive.\nYou must select a VS Code zip downloaded with this tool.",
                    icon: MessageBox.Avalonia.Enums.Icon.None).ShowDialog(programWindow.Window);
                return;
            }

            DoneText = "Valid VS Code Selected. Press Next to continue";
            EnableSelectionButtons = false;
            SetLocalForwardVisible(true);
        }

        private async Task DownloadVsCodeFunc()
        {
            var currentPlatform = PlatformUtils.CurrentPlatform;
            ProgressBar1Visible = true;
            ProgressBarAllVisible = true;

            var file = await programWindow.ShowFolderPicker("Select Directory For VS Code File", Environment.GetFolderPath(Environment.SpecialFolder.Personal));

            if (file == null)
            {
                return;
            }

            DoneText = "Downloading VS Code for all platforms. Please wait.";

            EnableSelectionButtons = false;
            SetLocalForwardVisible(false);

            var win64 = DownloadToMemoryStream(Platform.Win64, Model.Platforms[Platform.Win64].DownloadUrl, (d) => ProgressBar2 = d);
            var linux64 = DownloadToMemoryStream(Platform.Linux64, Model.Platforms[Platform.Linux64].DownloadUrl, (d) => ProgressBar3 = d);
            var mac64 = DownloadToMemoryStream(Platform.Mac64, Model.Platforms[Platform.Mac64].DownloadUrl, (d) => ProgressBar4 = d);

            var results = await Task.WhenAll(win64, linux64, mac64);

            string vscodeFileName = $"WPILib-VSCode-{Model.VSCodeVersion}.zip";

            string vscodeName = Path.Join(file, vscodeFileName);

            try
            {
                File.Delete(vscodeName);
            }
            catch
            {

            }

            using var archive = ZipFile.Open(vscodeName, ZipArchiveMode.Create);

            MemoryStream? ms = null;

            DoneText = "Copying Archives. Please wait.";
            foreach (var (stream, platform) in results)
            {
                var entry = archive.CreateEntry(Model.Platforms[platform].NameInZip);
                using var toWriteStream = entry.Open();
                stream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(toWriteStream);
                if (platform == currentPlatform)
                {
                    ms = stream;
                }
            }

            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);

                if (OperatingSystem.IsMacOS())
                {
                    Model.ToExtractArchiveMacOs = ms;
                }
                else
                {
                    Model.ToExtractArchive = OpenArchive(ms);
                }

                DoneText = $"Done Downloading. File is named {vscodeFileName} Press Next to continue";
                EnableSelectionButtons = true;
                SetLocalForwardVisible(true);
            }
        }

        private async Task DownloadSingleVSCodeFunc()
        {
            DoneText = "Downloading VS Code for current platform. Please wait.";
            Console.WriteLine("Single Download");
            var currentPlatform = PlatformUtils.CurrentPlatform;
            var url = Model.Platforms[currentPlatform].DownloadUrl;

            EnableSelectionButtons = false;
            SetLocalForwardVisible(false);
            ProgressBar1Visible = true;

            var (stream, platform) = await DownloadToMemoryStream(currentPlatform, url, (d) => ProgressBar1 = d);
            Console.WriteLine("Trying to open archive");

            if (OperatingSystem.IsMacOS())
            {
                Model.ToExtractArchiveMacOs = stream;
            }
            else
            {
                Model.ToExtractArchive = OpenArchive(stream);
            }

            DoneText = "Done Downloading. Press Next to continue";
            EnableSelectionButtons = true;
            SetLocalForwardVisible(true);
            refresher.RefreshForwardBackProperties();
        }

        private async Task<(MemoryStream stream, Platform platform)> DownloadToMemoryStream(Platform platform, string downloadUrl, Action<double> progressChanged)
        {
            MemoryStream ms = new MemoryStream(100000000);
            await DownloadForPlatform(downloadUrl, ms, progressChanged);
            return (ms, platform);
        }

        private async Task DownloadForPlatform(string downloadUrl, Stream outputStream, Action<double> progressChanged)
        {
            using var client = new HttpClientDownloadWithProgress(downloadUrl, outputStream);
            client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                if (progressPercentage != null)
                {
                    progressChanged?.Invoke(progressPercentage.Value);
                }
            };

            await client.StartDownload();
        }

        public override PageViewModelBase MoveNext()
        {
            return viewModelResolver.Resolve<InstallPageViewModel>();
        }
    }
}
