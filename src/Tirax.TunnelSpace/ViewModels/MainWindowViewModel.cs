using System;
using ReactiveUI;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    ViewModelBase currentViewModel;

    public MainWindowViewModel() {
        var initModel = new ConnectionSelectionViewModel();
        currentViewModel = initModel;

        initModel.NewConnectionCommand.Subscribe(_ => AddNewConnection());
    }

    public ViewModelBase CurrentViewModel
    {
        get => currentViewModel;
        set => this.RaiseAndSetIfChanged(ref currentViewModel, value);
    }

    void AddNewConnection() {
        CurrentViewModel = new TunnelConfigViewModel();
    }
}
