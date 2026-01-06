using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using WPILibInstaller.Models;
using static WPILibInstaller.Utils.ArchiveUtils;

namespace WPILibInstaller.Utils
{
    public static class VsCodeDownloadUtils
    {
        public static async Task<(MemoryStream stream, byte[] hash)> DownloadVsCodeForPlatformAsync(
            Platform currentPlatform,
            string downloadUrl,
            Action<double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            var ms = new MemoryStream();

            using (var client = new HttpClientDownloadWithProgress(downloadUrl, ms))
            {
                if (progressCallback is not null)
                {
                    client.ProgressChanged += (_, _, progressPercentage) =>
                    {
                        if (progressPercentage is double p)
                            progressCallback(p);
                    };
                }

                // If your downloader supports cancellation, prefer passing it through.
                // await client.StartDownload(cancellationToken);
                await client.StartDownload();
            }

            ms.Position = 0;

            using var sha = SHA256.Create();
            var hash = await sha.ComputeHashAsync(ms, cancellationToken);

            ms.Position = 0; // important: return stream ready to be read

            return (ms, hash);
        }

        public static void PrepareVsCodeModelForInstallation(VsCodeModel model, MemoryStream stream, Platform platform)
        {
            stream.Position = 0;

            if (platform == Platform.Mac64 || platform == Platform.MacArm64)
            {
                model.ToExtractArchiveMacOs = stream;
            }
            else
            {
                model.ToExtractArchive = OpenArchive(stream);
            }
        }
    }
}
