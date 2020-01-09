using Autofac;
using System;
using System.Collections.Generic;
using System.Text;

namespace WPILibInstaller_Avalonia.Interfaces
{
    public interface IDependencyInjection
    {
        IContainer Container { get; }

        public T Resolve<T>()
        {
            return Container.Resolve<T>();
        }
    }
}
