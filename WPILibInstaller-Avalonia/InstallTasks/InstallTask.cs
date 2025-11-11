using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Interfaces.Observer;

namespace WPILibInstaller.InstallTasks
{
    public abstract class InstallTask : ISubject
    {
        private int _progress;
        private int _progressTotal;
        private string _text = "";
        private string _textTotal = "";

        public int Progress
        {
            get => _progress;
            protected set
            {
                _progress = value;
                this.Notify();
            }
        }

        public int ProgressTotal
        {
            get => _progressTotal;
            protected set
            {
                _progressTotal = value;
                this.Notify();
            }
        }

        public string Text
        {
            get => _text;
            protected set
            {
                _text = value;
                this.Notify();
            }
        }

        public string TextTotal
        {
            get => _textTotal;
            protected set
            {
                _textTotal = value;
                this.Notify();
            }
        }

        // The subscription management methods and fields

        private List<IObserver> _observers = new List<IObserver>();

        public void Attach(IObserver observer)
        {
            this._observers.Add(observer);
        }

        public void Detach(IObserver observer)
        {
            this._observers.Remove(observer);
        }

        public void Notify()
        {
            foreach (var observer in _observers)
            {
                observer.Update(this);
            }
        }

        public abstract Task Execute(CancellationToken token);
    }
}
