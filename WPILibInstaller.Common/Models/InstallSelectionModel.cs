using CommunityToolkit.Mvvm.ComponentModel;

namespace WPILibInstaller.Models
{
    public partial class InstallSelectionModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UsesVsCode))]
        [NotifyPropertyChangedFor(nameof(NeedsDocumentation))]
        [NotifyPropertyChangedFor(nameof(HasAnySelection))]
        private bool installJava = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UsesVsCode))]
        [NotifyPropertyChangedFor(nameof(NeedsDocumentation))]
        [NotifyPropertyChangedFor(nameof(HasAnySelection))]
        private bool installCpp = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UsesVsCode))]
        [NotifyPropertyChangedFor(nameof(NeedsDocumentation))]
        [NotifyPropertyChangedFor(nameof(HasAnySelection))]
        private bool installPython;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NeedsDocumentation))]
        [NotifyPropertyChangedFor(nameof(HasAnySelection))]
        private bool installLabView;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AnyToolsSelected))]
        [NotifyPropertyChangedFor(nameof(HasAnySelection))]
        private bool installWPILibTools = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AnyToolsSelected))]
        [NotifyPropertyChangedFor(nameof(HasAnySelection))]
        private bool installElastic = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AnyToolsSelected))]
        [NotifyPropertyChangedFor(nameof(HasAnySelection))]
        private bool installAdvantageScope = true;

        public bool InstallAsAdmin { get; set; }

        public bool UsesVsCode => InstallJava || InstallCpp || InstallPython;

        public bool NeedsDocumentation => UsesVsCode || InstallLabView;

        public bool AnyToolsSelected => InstallWPILibTools || InstallElastic || InstallAdvantageScope;

        public bool HasAnySelection => InstallJava || InstallCpp || InstallPython || InstallLabView || AnyToolsSelected;
    }
}
