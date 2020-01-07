using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class StartPageViewModel : ViewModelBase, IRoutableViewModel
    {
        public IScreen HostScreen { get; }

        public string UrlPathSegment { get; } = "Start";

        public StartPageViewModel(IScreen screen) => HostScreen = screen;
    }
}
