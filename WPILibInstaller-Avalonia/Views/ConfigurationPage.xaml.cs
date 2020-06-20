using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using WPILibInstaller_Avalonia.ViewModels;

namespace WPILibInstaller_Avalonia.Views
{
    public class ConfigurationPage : ReactiveUserControl<ConfigurationPageViewModel>
    {
        public ConfigurationPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
