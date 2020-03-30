using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using WPILibInstaller_Avalonia.ViewModels;

namespace WPILibInstaller_Avalonia.Views
{
    public class InstallPage : ReactiveUserControl<InstallPageViewModel>
    {
        public InstallPage()
        {
            this.InitializeComponent();
            var vm = this.ViewModel;
            ;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
