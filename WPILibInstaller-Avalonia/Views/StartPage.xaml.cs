using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using WPILibInstaller.ViewModels;

namespace WPILibInstaller.Views
{
    public class StartPage : ReactiveUserControl<StartPageViewModel>
    {
        public StartPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
