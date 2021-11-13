using Avalonia.Controls;
using ReactiveUI;
using Splat;

namespace WPILibInstaller.ViewModels
{
    public abstract class PageViewModelBase : ReactiveObject
    {
        public string ForwardName { get; }

        public string BackName { get; }

        public virtual bool ForwardVisible => !string.IsNullOrWhiteSpace(ForwardName);
        public bool BackVisible => !string.IsNullOrWhiteSpace(BackName);

        protected PageViewModelBase(string forwardName, string backName)
        {
            ForwardName = forwardName;
            BackName = backName;
        }

        public abstract PageViewModelBase MoveNext();
    }
}
