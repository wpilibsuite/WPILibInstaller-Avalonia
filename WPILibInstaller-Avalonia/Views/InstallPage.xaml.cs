using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using WPILibInstaller.ViewModels;

namespace WPILibInstaller.Views
{
    public class InstallPage : ReactiveUserControl<InstallPageViewModel>
    {
        public InstallPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
