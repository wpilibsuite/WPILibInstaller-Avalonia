using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class StartPageViewModel : PageViewModelBase, IRoutableViewModel
    {
        public IScreen HostScreen { get; }

        public string UrlPathSegment { get; } = "Start";

        public StartPageViewModel(IScreen screen)
            : base("Start", "Back") => HostScreen = screen;
    }
}
