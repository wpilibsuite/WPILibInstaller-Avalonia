using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Interactivity;
using WPILibInstaller.ViewModels;
using Avalonia.VisualTree;
using System.Linq;
using Avalonia.Controls;
using System;

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
            Image wpilibImage = this.LogicalChildren[0].LogicalChildren.OfType<Image>().First();
            wpilibImage.Tapped += (o, e) => {
              ((StartPageViewModel)this.DataContext!).LogoClicked(o, e);
            };
        }
    }
}
