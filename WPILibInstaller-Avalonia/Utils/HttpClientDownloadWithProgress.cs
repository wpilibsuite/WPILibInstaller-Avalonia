using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WPILibInstaller_Avalonia.Utils
{
    class HttpClientDownloadWithProgress : IDisposable
    {
        private readonly string _downloadUrl;
        private readonly Stream _destinationStream;

        private HttpClient? _httpClient;

        public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

        public event ProgressChangedHandler? ProgressChanged;

        public HttpClientDownloadWithProgress(string downloadUrl, Stream output)
        {
            _downloadUrl = downloadUrl;
            _destinationStream = output;
        }

        public async Task<bool> StartDownload(CancellationToken token)
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromDays(1) };

            using var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            return await DownloadFileFromHttpResponseMessage(response, token);
        }

        private async Task<bool> DownloadFileFromHttpResponseMessage(HttpResponseMessage response, CancellationToken token)
        {
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            return await ProcessContentStream(totalBytes, contentStream, token);
        }

        private async Task<bool> ProcessContentStream(long? totalDownloadSize, Stream contentStream, CancellationToken token)
        {
            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = new byte[0xFFFF];
            var isMoreToRead = true;

            do
            {
                var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    isMoreToRead = false;
                    TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                    continue;
                }

                await _destinationStream.WriteAsync(buffer, 0, bytesRead);

                totalBytesRead += bytesRead;
                readCount += 1;

                if (readCount % 100 == 0)
                    TriggerProgressChanged(totalDownloadSize, totalBytesRead);
            }
            while (isMoreToRead && !token.IsCancellationRequested);
            if (token.IsCancellationRequested)
            {
                return false;
            }
            return true;
        }

        private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
        {
            if (ProgressChanged == null)
                return;

            double? progressPercentage = null;
            if (totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);

            ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
