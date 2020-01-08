using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WPILibInstaller_Avalonia.Models;
using WPILibInstaller_Avalonia.Utils;
using WPILibInstaller_Avalonia.Views;
using static WPILibInstaller_Avalonia.Models.VsCodeModel;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class VSCodePageViewModel : PageViewModelBase, IRoutableViewModel
    {
        public IScreen HostScreen => mainPage;

        private readonly MainWindowViewModel mainPage;

        public string UrlPathSegment { get; } = "vscode";

        public override bool ForwardVisible => forwardVisible;

        private bool forwardVisible = false;

        public string SelectText
        {
            get => selectText;
            set => this.RaiseAndSetIfChanged(ref selectText, value);
        }

        public string DownloadText
        {
            get => downloadText;
            set => this.RaiseAndSetIfChanged(ref downloadText, value);
        }


        public bool DownloadAllEnabled
        {
            get => downloadAllEnabled;
            set => this.RaiseAndSetIfChanged(ref downloadAllEnabled, value);
        }

        private bool downloadAllEnabled = true;

        public bool DownloadSingleEnabled
        {
            get => downloadSingleEnabled;
            set => this.RaiseAndSetIfChanged(ref downloadSingleEnabled, value);
        }

        private bool downloadSingleEnabled = true;

        public bool SelectExistingEnabled
        {
            get => selectExistingEnabled;
            set => this.RaiseAndSetIfChanged(ref selectExistingEnabled, value);
        }

        private bool selectExistingEnabled = true;

        public bool DoneVisible
        {
            get => doneVisible;
            set => this.RaiseAndSetIfChanged(ref doneVisible, value);
        }

        private bool doneVisible = false;

        private string selectText = "Select Existing VS Code Download";
        private string downloadText = "Download VS Code For All Platforms";

        public double ProgressBar1
        {
            get => progressBar1;
            set => this.RaiseAndSetIfChanged(ref progressBar1, value);
        }

        private double progressBar1 = 0;

        public double ProgressBar2
        {
            get => progressBar2;
            set => this.RaiseAndSetIfChanged(ref progressBar2, value);
        }

        private double progressBar2 = 0;

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

        public VsCodeModel Model { get; }

        private readonly MainWindow mainWindow;

        public VSCodePageViewModel(MainWindowViewModel screen, MainWindow mainWindow, VsCodeModel model)
            : base("Next", "Back")
        {
            mainPage = screen;
            this.mainWindow = mainWindow;
            Model = model;
        }

        public async Task SelectVsCode()
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            var files = await openDialog.ShowAsync(mainWindow);
            if (files.Length != 1) return;
            FileStream fs = new FileStream(files[0], FileMode.Open);
            using ZipArchive archive = new ZipArchive(fs);
            var currentPlatform = PlatformUtils.CurrentPlatform;
            var entry = archive.GetEntry(Model.Platforms[currentPlatform].NameInZip);
            MemoryStream ms = new MemoryStream(100000000);
            await entry.Open().CopyToAsync(ms);
            Model.ToExtractZipStream = ms;
            forwardVisible = true;
            DownloadSingleEnabled = false;
            DownloadAllEnabled = false;
            SelectExistingEnabled = false;
            DoneVisible = true;
            mainPage.RefreshForwardBackProperties();
        }

        public async void DownloadVsCode()
        {
            var currentPlatform = PlatformUtils.CurrentPlatform;

            DownloadSingleEnabled = false;
            DownloadAllEnabled = false;
            SelectExistingEnabled = false;


            var win32 = DownloadToMemoryStream(Platform.Win32, Model.Platforms[Platform.Win32].DownloadUrl, CancellationToken.None, (d) => ProgressBar1 = d);
            var win64 = DownloadToMemoryStream(Platform.Win64, Model.Platforms[Platform.Win64].DownloadUrl, CancellationToken.None, (d) => ProgressBar2 = d);
            var linux64 = DownloadToMemoryStream(Platform.Linux64, Model.Platforms[Platform.Linux64].DownloadUrl, CancellationToken.None, (d) => ProgressBar3 = d);
            var mac64 = DownloadToMemoryStream(Platform.Mac64, Model.Platforms[Platform.Mac64].DownloadUrl, CancellationToken.None, (d) => ProgressBar4 = d);

            var results = await Task.WhenAll(win32, win64, linux64, mac64);

            try
            {
                File.Delete("InstallerFiles.zip");
            }
            catch
            {

            }

            using var archive = ZipFile.Open("InstallerFiles.zip", ZipArchiveMode.Create);

            MemoryStream? ms = null;

            foreach (var (stream, platform) in results)
            {
                using var toWriteStream = archive.CreateEntry(Model.Platforms[platform].NameInZip).Open();
                await stream.CopyToAsync(toWriteStream);
                if (platform == currentPlatform)
                {
                    ms = stream;
                }
            }

            if (ms != null)
            {
                Model.ToExtractZipStream = ms;
                forwardVisible = true;
                DownloadSingleEnabled = false;
                DownloadAllEnabled = false;
                SelectExistingEnabled = false;
                DoneVisible = true;
                mainPage.RefreshForwardBackProperties();
            }

            
            
        }

        public async Task DownloadSingleVSCode()
        {
            var currentPlatform = PlatformUtils.CurrentPlatform;
            var url = Model.Platforms[currentPlatform].DownloadUrl;
            DownloadSingleEnabled = false;
            DownloadAllEnabled = false;
            SelectExistingEnabled = false;
            var memStream = await DownloadToMemoryStream(currentPlatform, url, CancellationToken.None, (d) => ProgressBar1 = d);
            if (memStream.stream != null)
            {
                Model.ToExtractZipStream = memStream.stream;
                forwardVisible = true;
                DownloadSingleEnabled = false;
                DownloadAllEnabled = false;
                SelectExistingEnabled = false;
                DoneVisible = true;
                mainPage.RefreshForwardBackProperties();
            }
            else
            {
                DownloadSingleEnabled = true;
                DownloadAllEnabled = true;
                SelectExistingEnabled = true;
                ; // TODO Fail
            }
        }

        private async Task<(MemoryStream? stream, Platform platform)> DownloadToMemoryStream(Platform platform, string downloadUrl, CancellationToken token, Action<double> progressChanged)
        {
            MemoryStream ms = new MemoryStream(100000000);
            var successful = await DownloadForPlatform(downloadUrl, ms, token, progressChanged);
            if (successful) return (ms, platform);
            return (null, platform);
        }

        private async Task<bool> DownloadForPlatform(string downloadUrl, Stream outputStream, CancellationToken token, Action<double> progressChanged)
        {
            using var client = new HttpClientDownloadWithProgress(downloadUrl, outputStream);
            client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                if (progressPercentage != null)
                {
                    progressChanged?.Invoke(progressPercentage.Value);
                }
            };

            return await client.StartDownload(token);
        }
    }
}
