using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WPILibInstaller_Avalonia.Interfaces
{
    public interface IProgramWindow
    {
        Task<string?> ShowFilePicker(string title, string? defaultPath = null);
        void CloseProgram();
    }
}
