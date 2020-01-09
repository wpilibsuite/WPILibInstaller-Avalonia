using ReactiveUI;
using System;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public abstract class PageViewModelBase : ReactiveObject, IRoutableViewModel
    {
        public string ForwardName { get; }

        public string BackName { get; }

        public virtual bool ForwardVisible => !string.IsNullOrWhiteSpace(ForwardName);
        public bool BackVisible => !string.IsNullOrWhiteSpace(BackName);

        public string UrlPathSegment { get; }

        public IScreen HostScreen { get; }

        protected PageViewModelBase(string forwardName, string backName, string urlPathSegment, IScreen screen)
        {
            ForwardName = forwardName;
            BackName = backName;
            UrlPathSegment = urlPathSegment;
            HostScreen = screen;
        }

        public abstract IObservable<IRoutableViewModel> MoveNext();

        public IObservable<IRoutableViewModel> MoveNext(IRoutableViewModel vm)
        {
            return HostScreen.Router.Navigate.Execute(vm);
        }
    }
}
