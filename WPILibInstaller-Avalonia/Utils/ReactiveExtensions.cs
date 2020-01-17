using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace WPILibInstaller_Avalonia.Utils
{
    public static class ReactiveExtensions
    {
        public static ReactiveCommand<Unit, Unit> CreateCatchableButton(Func<Task> toRun)
        {
            var command = ReactiveCommand.CreateFromTask(toRun);
            command.ThrownExceptions.Subscribe(GlobalHandler);
            return command;
        }

        public static void GlobalHandler(Exception e)
        {
            // Handle exception
            ;
        }
    }
}
