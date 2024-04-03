using ReactiveUI;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    ViewModelBase currentViewModel = new ConnectionSelectionViewModel();

    public ViewModelBase CurrentViewModel
    {
        get => currentViewModel;
        set => this.RaiseAndSetIfChanged(ref currentViewModel, value);
    }
}
