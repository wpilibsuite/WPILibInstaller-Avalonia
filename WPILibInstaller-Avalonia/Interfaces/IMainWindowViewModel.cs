using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace WPILibInstaller_Avalonia.Interfaces
{
    public interface IMainWindowViewModel
    {
        void RefreshForwardBackProperties();

        void HandleException(Exception e);

        IObservable<Unit> ExecuteGoNext();
    }
}
