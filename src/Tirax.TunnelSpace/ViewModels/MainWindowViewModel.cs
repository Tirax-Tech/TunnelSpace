using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;
using Tirax.TunnelSpace.EffHelpers;
using static Tirax.TunnelSpace.Effects.Prelude;

namespace Tirax.TunnelSpace.ViewModels;

public interface IAppMainWindow
{
    Eff<PageModelBase> CloseCurrentView { get; }
    Aff<PageModelBase> PushView(PageModelBase replacement);
    Eff<PageModelBase> Replace(PageModelBase replacement);
}

public sealed class MainWindowViewModel : ViewModelBase, IAppMainWindow
{
    readonly Stack<PageModelBase> history = new();
    string title = AppTitle;
    object header;

    static readonly string AppVersion =
        Assembly.GetEntryAssembly()!
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                .InformationalVersion
                .Split('+')[0];

    const string AppHeader = "Tunnel Space";
    static readonly string AppTitle = $"Tirax Tunnel Space {AppVersion}";

    static readonly Seq<string> ViewChangeProperties = Seq(nameof(CurrentViewModel), nameof(Header));

    static object CreateTitleText(string text) =>
        new TextBlock
        {
            Classes = { "Headline5" },
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = text
        };

    public MainWindowViewModel() {
        closeCurrentViewEff = Eff(history.Pop);
        CloseCurrentView = this.ChangeProperties(ViewChangeProperties, closeCurrentViewEff);
        history.Push(new LoadingScreenViewModel());
        header = CreateTitleText(AppHeader);
    }
    public string Title {
        get => title;
        set => this.RaiseAndSetIfChanged(ref title, value);
    }

    public object Header {
        get => header;
        set => this.RaiseAndSetIfChanged(ref header, value);
    }

    public PageModelBase CurrentViewModel {
        get => history.Peek();
        set => Replace(value).RunUnit();
    }

    public Aff<PageModelBase> PushView(PageModelBase view) =>
        from _1 in UiEff(() => header = GetViewHeader(view))
        from v in this.ChangeProperty(nameof(CurrentViewModel), PushViewEff(view))
        select v;

    static object GetViewHeader(PageModelBase view) =>
        view.Header switch
        {
            null     => CreateTitleText(AppHeader),
            string s => CreateTitleText(s),
            _        => view.Header
        };

    Eff<PageModelBase> PushViewEff(PageModelBase view) =>
        Eff(() => {
                history.Push(view);
                return view;
            });

    public Eff<PageModelBase> CloseCurrentView { get; }
    readonly Eff<PageModelBase> closeCurrentViewEff;

    public Eff<PageModelBase> Replace(PageModelBase replacement) =>
        this.ChangeProperties(ViewChangeProperties,
                        from current in closeCurrentViewEff
                        from _ in PushViewEff(replacement)
                        select current);
}