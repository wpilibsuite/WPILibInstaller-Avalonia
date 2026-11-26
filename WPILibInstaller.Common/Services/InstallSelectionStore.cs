using System.Text.Json;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.Services
{
    /// <summary>
    /// Reads and writes the component selection next to the installed files. A missing
    /// or unreadable file means the caller should fall back to selecting everything.
    /// </summary>
    public sealed class InstallSelectionStore : IInstallSelectionStore
    {
        private const string SelectionFileName = "installSelection.json";

        private readonly IConfigurationProvider configurationProvider;

        public InstallSelectionStore(IConfigurationProvider configurationProvider)
        {
            this.configurationProvider = configurationProvider;
        }

        public InstallSelectionSnapshot? Load()
        {
            try
            {
                var path = GetSelectionPath();
                if (!File.Exists(path))
                {
                    return null;
                }

                return JsonSerializer.Deserialize(
                    File.ReadAllText(path),
                    SourceGenerationContext.Default.InstallSelectionSnapshot);
            }
            catch (Exception ex) when (ex is IOException
                                          or UnauthorizedAccessException
                                          or JsonException
                                          or NotSupportedException
                                          or ArgumentException
                                          or InvalidOperationException
                                          or NullReferenceException)
            {
                // Treat an unreadable selection file the same as a missing one.
                return null;
            }
        }

        public async Task SaveAsync(InstallSelectionModel model, CancellationToken token = default)
        {
            try
            {
                var path = GetSelectionPath();
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(
                    InstallSelectionSnapshot.FromModel(model),
                    SourceGenerationContext.Default.InstallSelectionSnapshot);
                await File.WriteAllTextAsync(path, json, token);
            }
            catch (Exception ex) when (ex is IOException
                                          or UnauthorizedAccessException
                                          or JsonException
                                          or NotSupportedException
                                          or ArgumentException
                                          or InvalidOperationException
                                          or NullReferenceException)
            {
                // Persisting the selection is best effort; the install itself succeeded.
            }
        }

        private string GetSelectionPath()
        {
            var installDirectory = configurationProvider.InstallDirectory;
            if (string.IsNullOrWhiteSpace(installDirectory))
            {
                throw new InvalidOperationException("The install directory is not available yet.");
            }

            return Path.Combine(installDirectory, SelectionFileName);
        }
    }
}
