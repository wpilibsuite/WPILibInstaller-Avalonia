using System;
using System.Collections.Generic;
using System.Text;
using WPILibInstaller_Avalonia.Models;

namespace WPILibInstaller_Avalonia.Interfaces
{
    public interface IVsCodeModelProvider
    {
        VsCodeModel GetVsCodeModel();
    }
}
