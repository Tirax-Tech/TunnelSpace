using System;
using System.Collections.Generic;
using ReactiveUI;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    readonly ServiceProviderEff sp;
    readonly Stack<ViewModelBase> history = new();

    public MainWindowViewModel(ServiceProviderEff sp) {
        this.sp = sp;
        var initModel = new ConnectionSelectionViewModel();
        history.Push(initModel);

        initModel.NewConnectionCommand.Subscribe(_ => AddNewConnection());
    }

    public ViewModelBase CurrentViewModel {
        get => history.Peek();
        set {
            var vm = history.Peek();
            this.RaiseAndSetIfChanged(ref vm, value);
            history.Pop();
            history.Push(value);
        }
    }

    void AddNewConnection() {
        var vm = CurrentViewModel;
        var newView = sp.GetRequiredService<TunnelConfigViewModel>().Run().ThrowIfFail();
        history.Push(newView);

        newView.Save.Subscribe(_ => {
            this.RaisePropertyChanging(nameof(CurrentViewModel));
            history.Pop();
            this.RaisePropertyChanged(nameof(CurrentViewModel));
        });

        this.RaiseAndSetIfChanged(ref vm, newView, nameof(CurrentViewModel));
    }
}
