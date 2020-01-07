using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class ConfigurationPageViewModel : ViewModelBase, IRoutableViewModel
    {
        public IScreen HostScreen { get; }

        public string UrlPathSegment { get; } = "configuration";

        public ConfigurationPageViewModel(IScreen screen) => HostScreen = screen;
    }
}
