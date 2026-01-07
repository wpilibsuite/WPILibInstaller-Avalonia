namespace WPILibInstaller.Models
{
    /// <summary>
    ///  Represents the progress of an installation process, including the percentage completed and a status message.
    /// </summary>
    /// <param name="Percentage">The percentage of the installation that has been completed.</param>
    /// <param name="StatusText">A message describing the current status of the installation.</param>
    public record InstallProgress(int Percentage, string StatusText);
}
