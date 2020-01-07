using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class ConfigurationPageViewModel : PageViewModelBase, IRoutableViewModel
    {
        public IScreen HostScreen { get; }

        public string UrlPathSegment { get; } = "configuration";

        public ConfigurationPageViewModel(IScreen screen)
            : base("Install", "Back") => HostScreen = screen;
    }
}
