using Avalonia.Controls;
using System.Threading.Tasks;

namespace WPILibInstaller_Avalonia.Interfaces
{
    public interface IProgramWindow
    {
        Task<string?> ShowFilePicker(string title, string? defaultPath = null);
        void CloseProgram();

        Window Window { get; }
    }
}
