using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using WPILibInstaller.ViewModels;

namespace WPILibInstaller.Views
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
