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
using WPILibInstaller_Avalonia.Utils;
using WPILibInstaller_Avalonia.Views;

namespace WPILibInstaller_Avalonia.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IMainWindowViewModel, ICatchableButtonFactory
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

        public ReactiveCommand<Unit, Unit> GoNext { get; }

        public ReactiveCommand<Unit, Unit> GoBack { get; }

        public void HandleException(Exception e)
        {
            var cancelPage = viewModelResolver.Resolve<CanceledPageViewModel>();
            cancelPage.SetException(e);
            CurrentPage = cancelPage;
        }

        private Task GoNextFunc()
        {
            HandleStateChange();
            return Task.CompletedTask;
        }

        private Task GoBackFunc()
        {
            pages.Pop();
            CurrentPage = pages.Pop();
            return Task.CompletedTask;
        }

        public void RefreshForwardBackProperties()
        {
            this.RaisePropertyChanged(nameof(ForwardName));
            this.RaisePropertyChanged(nameof(BackName));
            this.RaisePropertyChanged(nameof(ForwardVisible));
            this.RaisePropertyChanged(nameof(BackVisible));
        }

        private readonly IViewModelResolver viewModelResolver;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public MainWindowViewModel(IViewModelResolver viewModelResolver)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        {
            this.WhenAnyValue(x => x.CurrentPage)
                .Subscribe(o => RefreshForwardBackProperties());

            GoNext = CreateCatchableButton(GoNextFunc);
            GoBack = CreateCatchableButton(GoBackFunc);


            this.viewModelResolver = viewModelResolver;
        }

        public ReactiveCommand<Unit, Unit> CreateCatchableButton(Func<Task> toRun)
        {
            var command = ReactiveCommand.CreateFromTask(toRun);
            command.ThrownExceptions.Subscribe(HandleException);
            return command;
        }

        public void Initialize()
        {
            CurrentPage = viewModelResolver.Resolve<StartPageViewModel>();
        }

        private void HandleStateChange()
        {
            CurrentPage = CurrentPage.MoveNext();
        }

        public IObservable<Unit> ExecuteGoNext()
        {
            return GoNext.Execute();
        }
    }
}
