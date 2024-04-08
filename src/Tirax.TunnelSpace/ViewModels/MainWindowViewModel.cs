using System.Collections.Generic;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    readonly Stack<ViewModelBase> history = new();

    // for designer
    public MainWindowViewModel() : this(new LoadingScreenViewModel()) {}

    public MainWindowViewModel(ViewModelBase initialView) {
        CloseCurrentViewEff = Eff(history.Pop);
        CloseCurrentView = this.ChangeProperty(nameof(CurrentViewModel), CloseCurrentViewEff);
        history.Push(initialView);
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

    public readonly Eff<ViewModelBase> CloseCurrentView;
    readonly Eff<ViewModelBase> CloseCurrentViewEff;

    Eff<ViewModelBase> Replace(ViewModelBase replacement) =>
        this.ChangeProperty(nameof(CurrentViewModel),
                        from current in CloseCurrentViewEff
                        from _ in PushViewEff(replacement)
                        select current);
}