﻿using System;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using WPILibInstaller.ViewModels;

namespace WPILibInstaller.Views
{
    public class DeprecatedOsPage : ReactiveUserControl<DeprecatedOsPageViewModel>
    {
        public DeprecatedOsPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
