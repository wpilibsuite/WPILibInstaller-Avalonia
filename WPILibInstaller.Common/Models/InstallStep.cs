namespace WPILibInstaller.Models
{
    public enum InstallStepKind
    {
        StopGradleDaemons,
        ExtractArchive,
        ExtractSelectedTools,
        GradleSetup,
        ToolSetup,
        CppSetup,
        MavenFixer,
        VsCodeSetup,
        VsCodeSettings,
        VsCodeExtensions,
        Shortcuts,
    }

    public sealed record InstallStep(InstallStepKind Kind, Func<Task> Run);
}
