using System.Collections.Generic;
using Avalonia.Controls;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Views;

namespace Tirax.TunnelSpace.ViewModels;

public interface IAppMainWindow
{
    Eff<Window> CreateView { get; }

    Eff<ViewModelBase> CloseCurrentView { get; }
    Eff<ViewModelBase> PushView(ViewModelBase replacement);
    Eff<ViewModelBase> Replace(ViewModelBase replacement);
}

public sealed class MainWindowViewModel : ViewModelBase, IAppMainWindow
{
    readonly Stack<ViewModelBase> history = new();

    public MainWindowViewModel() {
        closeCurrentViewEff = Eff(history.Pop);
        CloseCurrentView = this.ChangeProperty(nameof(CurrentViewModel), closeCurrentViewEff);
        history.Push(new LoadingScreenViewModel());
    }

    public ViewModelBase CurrentViewModel {
        get => history.Peek();
        set => Replace(value).RunUnit();
    }

    public Eff<ViewModelBase> PushView(ViewModelBase view) =>
        this.ChangeProperty(nameof(CurrentViewModel), PushViewEff(view));

    Eff<ViewModelBase> PushViewEff(ViewModelBase view) =>
        Eff(() => {
            history.Push(view);
            return view;
        });

    public Eff<Window> CreateView => Eff(() => (Window) new MainWindow { DataContext = this });

    public Eff<ViewModelBase> CloseCurrentView { get; }
    readonly Eff<ViewModelBase> closeCurrentViewEff;

    public Eff<ViewModelBase> Replace(ViewModelBase replacement) =>
        this.ChangeProperty(nameof(CurrentViewModel),
                        from current in closeCurrentViewEff
                        from _ in PushViewEff(replacement)
                        select current);
}