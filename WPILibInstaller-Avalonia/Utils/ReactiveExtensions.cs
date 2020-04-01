using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using WPILibInstaller_Avalonia.Interfaces;
using WPILibInstaller_Avalonia.ViewModels;

namespace WPILibInstaller_Avalonia.Utils
{
    public static class ReactiveExtensions
    {
        public static MainWindowViewModel? MainWindowVM { get; set; }

        public static ReactiveCommand<Unit, Unit> CreateCatchableButton(Func<Task> toRun)
        {
            var command = ReactiveCommand.CreateFromTask(toRun);
            command.ThrownExceptions.Subscribe(MainWindowVM!.HandleException);
            return command;
        }
    }
}
