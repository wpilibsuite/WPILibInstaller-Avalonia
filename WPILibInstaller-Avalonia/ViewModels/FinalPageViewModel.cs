using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class FinalPageViewModel : PageViewModelBase, IRoutableViewModel
    {
        public IScreen HostScreen { get; }

        public string UrlPathSegment { get; } = "Finish";

        public FinalPageViewModel(IScreen screen)
            : base("Finish", "") => HostScreen = screen;
    }
}
