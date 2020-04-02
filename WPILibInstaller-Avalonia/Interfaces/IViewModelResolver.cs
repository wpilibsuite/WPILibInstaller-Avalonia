using Autofac;
using System;
using System.Collections.Generic;
using System.Text;
using WPILibInstaller_Avalonia.ViewModels;

namespace WPILibInstaller_Avalonia.Interfaces
{
    public interface IViewModelResolver
    {

        T Resolve<T>() where T : notnull, PageViewModelBase;

        IMainWindowViewModel ResolveMainWindow();
    }
}
