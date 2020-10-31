using Avalonia.Controls;
using System.Threading.Tasks;

namespace WPILibInstaller.Interfaces
{
    public interface IProgramWindow
    {
        Task<string?> ShowFilePicker(string title, string? defaultPath = null);
        Task<string?> ShowFolderPicker(string title, string? initialiDirectory = null);
        void CloseProgram();

        Window Window { get; }
    }
}
