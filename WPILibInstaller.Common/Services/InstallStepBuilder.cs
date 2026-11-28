using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;

namespace WPILibInstaller.Services
{
    /// <summary>
    /// Builds the gated install sequence shared by the GUI and CLI. Labels are supplied by
    /// each caller so both front ends can keep their existing progress text.
    /// </summary>
    public static class InstallStepBuilder
    {
        public static IReadOnlyList<InstallStep> Build(
            InstallSelectionModel selection,
            IArchiveExtractionService archiveExtractionService,
            IVsCodeInstallationService vsCodeInstallationService,
            IToolInstallationService toolInstallationService,
            IShortcutService shortcutService,
            CancellationToken token,
            IProgress<InstallProgress>? progress = null)
        {
            List<InstallStep> steps = [];

            void Add(InstallStepKind kind, Func<Task> run) => steps.Add(new InstallStep(kind, run));

            bool anyLanguage = selection.AnyLanguage;

            if (anyLanguage)
            {
                Add(InstallStepKind.StopGradleDaemons, () => toolInstallationService.StopGradleDaemons(progress, token));
            }

            if (anyLanguage)
            {
                Add(InstallStepKind.ExtractArchive, () => archiveExtractionService.ExtractArchive(token, null, progress));
            }
            else if (selection.AnyToolsSelected)
            {
                Add(InstallStepKind.ExtractSelectedTools, () => archiveExtractionService.ExtractSelectedTools(
                    selection.InstallWPILibTools,
                    selection.InstallElastic,
                    selection.InstallAdvantageScope,
                    token,
                    progress));
            }

            if (anyLanguage)
            {
                Add(InstallStepKind.GradleSetup, () => toolInstallationService.RunGradleSetup(progress));
            }

            if (selection.AnyToolsSelected)
            {
                Add(InstallStepKind.ToolSetup, () => toolInstallationService.RunToolSetup(
                    selection.InstallWPILibTools,
                    selection.InstallElastic,
                    selection.InstallAdvantageScope,
                    progress));
            }

            if (selection.InstallCpp)
            {
                Add(InstallStepKind.CppSetup, () => toolInstallationService.RunCppSetup(progress));
            }

            if (anyLanguage)
            {
                Add(InstallStepKind.MavenFixer, () => toolInstallationService.RunMavenMetaDataFixer(progress));
            }

            if (selection.UsesVsCode)
            {
                Add(InstallStepKind.VsCodeSetup, () => vsCodeInstallationService.RunVsCodeSetup(token, progress));
                Add(InstallStepKind.VsCodeSettings, () => vsCodeInstallationService.ConfigureVsCodeSettings());
                Add(InstallStepKind.VsCodeExtensions, () => vsCodeInstallationService.RunVsCodeExtensionsSetup(progress));
            }

            if (selection.HasAnySelection)
            {
                Add(InstallStepKind.Shortcuts, () => shortcutService.RunShortcutCreator(token));
            }

            return steps;
        }
    }
}
