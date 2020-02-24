using Avalonia.Controls;
using DynamicData;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using WPILibInstaller_Avalonia.Interfaces;
using WPILibInstaller_Avalonia.Models;
using WPILibInstaller_Avalonia.Views;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IMainWindowViewModelRefresher
    {
        private PageViewModelBase currentPage;
        public PageViewModelBase CurrentPage
        {
            get => currentPage;
            set
            {
                pages.Push(value);
                this.RaiseAndSetIfChanged(ref currentPage, value);
            }
        }

        private readonly Stack<PageViewModelBase> pages = new Stack<PageViewModelBase>();

        public string? ForwardName => CurrentPage?.ForwardName;

        public string? BackName => CurrentPage?.BackName;

        public bool ForwardVisible => CurrentPage?.ForwardVisible ?? false;

        public bool BackVisible => CurrentPage?.BackVisible ?? false;

        public void GoNext()
        {
            HandleStateChange();
        }

        public void GoBack()
        {
            pages.Pop();
            CurrentPage = pages.Pop();
        }

        public void RefreshForwardBackProperties()
        {
            this.RaisePropertyChanged(nameof(ForwardName));
            this.RaisePropertyChanged(nameof(BackName));
            this.RaisePropertyChanged(nameof(ForwardVisible));
            this.RaisePropertyChanged(nameof(BackVisible));
        }

        private readonly IDependencyInjection di;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public MainWindowViewModel(IDependencyInjection di)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        {
            this.WhenAnyValue(x => x.CurrentPage)
                .Subscribe(o => RefreshForwardBackProperties());

            this.di = di;
        }

        public void Initialize()
        {
            CurrentPage = di.Resolve<StartPageViewModel>();
        }

        private void HandleStateChange()
        {
            CurrentPage = CurrentPage.MoveNext();
        }
    }
}
