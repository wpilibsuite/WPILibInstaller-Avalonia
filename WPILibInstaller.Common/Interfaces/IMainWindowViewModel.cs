using System;
using System.Reactive;

namespace WPILibInstaller.Interfaces
{
    public interface IMainWindowViewModel
    {
        void RefreshForwardBackProperties();

        void HandleException(Exception e);

        IObservable<Unit> ExecuteGoNext();
    }
}
