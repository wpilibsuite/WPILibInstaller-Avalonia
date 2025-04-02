using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ReactiveUI;
using WPILibInstaller.Interfaces;
using WPILibInstaller.Models;
using WPILibInstaller.Utils;

namespace WPILibInstaller.ViewModels
{
    public class DeprecatedOsPageViewModel : PageViewModelBase
    {
        private readonly IViewModelResolver viewModelResolver;

        public DeprecatedOsPageViewModel(IViewModelResolver viewModelResolver)
            : base("I Understand", "")
        {
            this.viewModelResolver = viewModelResolver;
            var version = Environment.OSVersion;
            var bitness = IntPtr.Size == 8 ? "64 Bit" : "32 Bit";
            CurrentSystem = $"Detected {version.VersionString} {bitness}";
        }

        public string CurrentSystem { get; }

        public override PageViewModelBase MoveNext()
        {
            return viewModelResolver.Resolve<StartPageViewModel>();
        }
    }
}
