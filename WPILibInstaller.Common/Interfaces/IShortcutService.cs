using System.Threading;
using System.Threading.Tasks;

namespace WPILibInstaller.Interfaces
{
    public interface IShortcutService
    {
        Task RunShortcutCreator(CancellationToken token);
    }
}
