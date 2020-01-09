using System;
using System.Collections.Generic;
using System.Text;
using WPILibInstaller_Avalonia.Models;

namespace WPILibInstaller_Avalonia.Interfaces
{
    public interface IConfigurationProvider
    {
        VsCodeModel GetVsCodeModel();

        string GetInstallDirectory();
    }
}
