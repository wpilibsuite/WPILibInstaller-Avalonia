﻿using System;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;

namespace WPILibInstaller.Interfaces
{
    public interface ICatchableButtonFactory
    {
        ReactiveCommand<Unit, Unit> CreateCatchableButton(Func<Task> toRun);
    }
}
