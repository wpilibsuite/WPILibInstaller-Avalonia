using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WPILibInstaller_Avalonia.Models;
using WPILibInstaller_Avalonia.Utils;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class VSCodePageViewModel : PageViewModelBase, IRoutableViewModel
    {
        public IScreen HostScreen => mainPage;

        private MainWindowViewModel mainPage;

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

        public VSCodeModel Model { get; }

        public VSCodePageViewModel(MainWindowViewModel screen, VSCodeModel model)
            : base("Next", "Back")
        {
            mainPage = screen;
            Model = model;
        }

        public async Task SelectVsCode()
        {
            
        }

        public async Task DownloadVsCode()
        {

        }

        public async Task DownloadSingleVSCode()
        {
            var currentPlatform = PlatformUtils.CurrentPlatform;
            var url = Model.Platforms[currentPlatform].DownloadUrl;
            DownloadSingleEnabled = false;
            DownloadAllEnabled = false;
            SelectExistingEnabled = false;
            MemoryStream ms = new MemoryStream(100000000);
            var successful = await DownloadForPlatform(url, ms, CancellationToken.None, (d) => 
            {
                ProgressBar1 = d;
            });
            if (successful)
            {
                Model.ToExtractZipStream = ms;
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
