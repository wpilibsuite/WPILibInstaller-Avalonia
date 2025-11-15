using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace WPILibInstaller.Utils
{
    class HttpClientDownloadWithProgress : IDisposable
    {
        private readonly string _downloadUrl;
        private readonly Stream _destinationStream;

        private HttpClient? _httpClient;

        // ORIGINAL delegate & event (unchanged)
        public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

        public event ProgressChangedHandler? ProgressChanged;

        // NEW: delegate & event that provide raw byte counts (useful for Spectre.Console progress)
        // Signature: total file size (nullable) and bytes downloaded so far.
        public delegate void ProgressBytesChangedHandler(long? totalFileSize, long totalBytesDownloaded);

        public event ProgressBytesChangedHandler? ProgressBytesChanged;

        public HttpClientDownloadWithProgress(string downloadUrl, Stream output)
        {
            _downloadUrl = downloadUrl;
            _destinationStream = output;
        }

        public async Task StartDownload()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromDays(1) };

            using var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            await DownloadFileFromHttpResponseMessage(response);
        }

        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            await ProcessContentStream(totalBytes, contentStream);
        }

        private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
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
            while (isMoreToRead);
        }

        private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
        {
            // Compute percentage (this preserves existing behavior)
            double? progressPercentage = null;
            if (totalDownloadSize.HasValue && totalDownloadSize.Value > 0)
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);

            // Invoke original event (unchanged signature)
            ProgressChanged?.Invoke(totalDownloadSize, totalBytesRead, progressPercentage);

            // Invoke new raw-bytes event (useful for Spectre.Console)
            ProgressBytesChanged?.Invoke(totalDownloadSize, totalBytesRead);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
