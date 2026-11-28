namespace WPILibInstaller.Models
{
    /// <summary>
    /// The seven component flags shared by the in-memory selection model and its on-disk
    /// snapshot. Keeping one copy of the mapping prevents the two representations from
    /// gaining different fields later.
    /// </summary>
    public interface IInstallSelectionValues
    {
        bool InstallJava { get; set; }

        bool InstallCpp { get; set; }

        bool InstallPython { get; set; }

        bool InstallLabView { get; set; }

        bool InstallWPILibTools { get; set; }

        bool InstallElastic { get; set; }

        bool InstallAdvantageScope { get; set; }
    }

    internal static class InstallSelectionValues
    {
        public static void Copy(IInstallSelectionValues source, IInstallSelectionValues target)
        {
            target.InstallJava = source.InstallJava;
            target.InstallCpp = source.InstallCpp;
            target.InstallPython = source.InstallPython;
            target.InstallLabView = source.InstallLabView;
            target.InstallWPILibTools = source.InstallWPILibTools;
            target.InstallElastic = source.InstallElastic;
            target.InstallAdvantageScope = source.InstallAdvantageScope;
        }
    }
}
