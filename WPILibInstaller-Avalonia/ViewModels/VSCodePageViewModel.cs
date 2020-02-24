using Avalonia.Controls;
using MessageBox.Avalonia;
using ReactiveUI;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WPILibInstaller_Avalonia.Interfaces;
using WPILibInstaller_Avalonia.Models;
using WPILibInstaller_Avalonia.Utils;
using WPILibInstaller_Avalonia.Views;
using static WPILibInstaller_Avalonia.Models.VsCodeModel;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class VSCodePageViewModel : PageViewModelBase, IVsCodeInstallLocationProvider
    {

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

        public string DoneText
        {
            get => doneText;
            set => this.RaiseAndSetIfChanged(ref doneText, value);
        }

        private string doneText = "Done! Click Next To Continue";

        public VsCodeModel Model { get; }

        public bool AlreadyInstalled { get; }

        private readonly IProgramWindow programWindow;
        private readonly IMainWindowViewModelRefresher refresher;
        private readonly IDependencyInjection di;

        public VSCodePageViewModel(IMainWindowViewModelRefresher mainRefresher, IProgramWindow programWindow, IConfigurationProvider modelProvider, IDependencyInjection di)
            : base("Next", "Back")
        {
            this.refresher = mainRefresher;
            this.programWindow = programWindow;
            Model = modelProvider.VsCodeModel;
            this.di = di;

            forwardVisible = true;
            refresher.RefreshForwardBackProperties();

            // Check to see if VS Code is already installed
            var rootPath = modelProvider.InstallDirectory;
            var vscodePath = Path.Join(rootPath, "vscode");
            if (Directory.Exists(vscodePath))
            {
                DoneText = "VS Code Already Installed. You can either download to reinstall, or click Next to skip";
                DoneVisible = true;
                forwardVisible = true;
                refresher.RefreshForwardBackProperties();
                AlreadyInstalled = true;
            }
        }

        public async Task SelectVsCode()
        {
            var file = await programWindow.ShowFilePicker("Select VS Code");
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
                await entry.Open().CopyToAsync(ms);

                Model.ToExtractArchive = ZipArchive.Open(ms);
            }
            catch
            {
                await MessageBoxManager.GetMessageBoxStandardWindow("Error",
                    "Correct VS Code not found in archive", icon: MessageBox.Avalonia.Enums.Icon.None).ShowDialog(programWindow.Window);
                return;
            }

            forwardVisible = true;
            DownloadSingleEnabled = false;
            DownloadAllEnabled = false;
            SelectExistingEnabled = false;
            DoneVisible = true;
            refresher.RefreshForwardBackProperties();
        }

        public async void DownloadVsCode()
        {
            await Task.Yield();
            //var currentPlatform = PlatformUtils.CurrentPlatform;

            //DownloadSingleEnabled = false;
            //DownloadAllEnabled = false;
            //SelectExistingEnabled = false;
            //forwardVisible = false;
            //refresher.RefreshForwardBackProperties();

            //var win32 = DownloadToMemoryStream(Platform.Win32, Model.Platforms[Platform.Win32].DownloadUrl, CancellationToken.None, (d) => ProgressBar1 = d);
            //var win64 = DownloadToMemoryStream(Platform.Win64, Model.Platforms[Platform.Win64].DownloadUrl, CancellationToken.None, (d) => ProgressBar2 = d);
            //var linux64 = DownloadToMemoryStream(Platform.Linux64, Model.Platforms[Platform.Linux64].DownloadUrl, CancellationToken.None, (d) => ProgressBar3 = d);
            //var mac64 = DownloadToMemoryStream(Platform.Mac64, Model.Platforms[Platform.Mac64].DownloadUrl, CancellationToken.None, (d) => ProgressBar4 = d);

            //var results = await Task.WhenAll(win32, win64, linux64, mac64);

            //try
            //{
            //    File.Delete("InstallerFiles.zip");
            //}
            //catch
            //{

            //}

            //using var archive = ZipFile.Open("InstallerFiles.zip", ZipArchiveMode.Create);

            //MemoryStream? ms = null;

            //foreach (var (stream, platform) in results)
            //{
            //    using var toWriteStream = archive.CreateEntry(Model.Platforms[platform].NameInZip).Open();
            //    await stream.CopyToAsync(toWriteStream);
            //    if (platform == currentPlatform)
            //    {
            //        ms = stream;
            //    }
            //}

            //if (ms != null)
            //{
            //    Model.ToExtractZipStream = ms;
            //    forwardVisible = true;
            //    DownloadSingleEnabled = false;
            //    DownloadAllEnabled = false;
            //    SelectExistingEnabled = false;
            //    DoneVisible = true;
            //    refresher.RefreshForwardBackProperties();
            //}



        }

        public async Task DownloadSingleVSCode()
        {
            var currentPlatform = PlatformUtils.CurrentPlatform;
            var url = Model.Platforms[currentPlatform].DownloadUrl;
            DownloadSingleEnabled = false;
            DownloadAllEnabled = false;
            SelectExistingEnabled = false;
            forwardVisible = false;
            refresher.RefreshForwardBackProperties();
            var (stream, platform) = await DownloadToMemoryStream(currentPlatform, url, CancellationToken.None, (d) => ProgressBar1 = d);
            if (stream != null)
            {
                Model.ToExtractArchive = ZipArchive.Open(stream);
                forwardVisible = true;
                DownloadSingleEnabled = false;
                DownloadAllEnabled = false;
                SelectExistingEnabled = false;
                DoneVisible = true;
                refresher.RefreshForwardBackProperties();
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

        public override PageViewModelBase MoveNext()
        {
            var configPage = di.Resolve<ConfigurationPageViewModel>();
            configPage.UpdateVsSettings();
            return configPage;
        }
    }
}
