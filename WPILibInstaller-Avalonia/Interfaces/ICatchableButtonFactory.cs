using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace WPILibInstaller_Avalonia.Interfaces
{
    public interface ICatchableButtonFactory
    {
        ReactiveCommand<Unit, Unit> CreateCatchableButton(Func<Task> toRun);
    }
}
