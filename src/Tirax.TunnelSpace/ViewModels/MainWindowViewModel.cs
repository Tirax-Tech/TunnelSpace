using System.Collections.Generic;
using System.Reflection;
using ReactiveUI;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace.ViewModels;

public interface IAppMainWindow
{
    Eff<ViewModelBase> CloseCurrentView { get; }
    Eff<ViewModelBase> PushView(ViewModelBase replacement);
    Eff<ViewModelBase> Replace(ViewModelBase replacement);
}

public sealed class MainWindowViewModel : ViewModelBase, IAppMainWindow
{
    readonly Stack<ViewModelBase> history = new();
    string title = AppTitle;

    static readonly string AppVersion =
        Assembly.GetEntryAssembly()!
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                .InformationalVersion
                .Split('+')[0];

    static readonly string AppTitle = $"Tirax Tunnel Space {AppVersion}";

    public MainWindowViewModel() {
        closeCurrentViewEff = Eff(history.Pop);
        CloseCurrentView = this.ChangeProperty(nameof(CurrentViewModel), closeCurrentViewEff);
        history.Push(new LoadingScreenViewModel());
    }
    public string Title {
        get => title;
        set => this.RaiseAndSetIfChanged(ref title, value);
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

    public Eff<ViewModelBase> CloseCurrentView { get; }
    readonly Eff<ViewModelBase> closeCurrentViewEff;

    public Eff<ViewModelBase> Replace(ViewModelBase replacement) =>
        this.ChangeProperty(nameof(CurrentViewModel),
                        from current in closeCurrentViewEff
                        from _ in PushViewEff(replacement)
                        select current);
}