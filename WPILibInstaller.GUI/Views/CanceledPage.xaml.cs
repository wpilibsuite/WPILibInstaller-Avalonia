using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using WPILibInstaller.ViewModels;

namespace WPILibInstaller.Views
{
    public class CanceledPage : ReactiveUserControl<CanceledPageViewModel>
    {
        public CanceledPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
