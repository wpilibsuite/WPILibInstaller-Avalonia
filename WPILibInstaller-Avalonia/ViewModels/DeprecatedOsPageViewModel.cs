using MessageBox.Avalonia.DTO;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
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
        }

        public override PageViewModelBase MoveNext()
        {
            return viewModelResolver.Resolve<StartPageViewModel>();
        }
    }
}
